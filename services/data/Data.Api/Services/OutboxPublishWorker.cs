using System.Text.Json;
using Confluent.Kafka;
using Data.Api.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Data.Api.Services;

public sealed class OutboxPublishWorker : BackgroundService
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IProducer<string, string> _producer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly IOptions<OutboxPublishOptions> _publishOptions;
    private readonly OutboxPublishState _state;
    private readonly OutboxPublishControl _control;
    private readonly ILogger<OutboxPublishWorker> _logger;

    public OutboxPublishWorker(
        IConnectionStringProvider connectionStringProvider,
        IProducer<string, string> producer,
        IOptions<KafkaOptions> kafkaOptions,
        IOptions<OutboxPublishOptions> publishOptions,
        OutboxPublishState state,
        OutboxPublishControl control,
        ILogger<OutboxPublishWorker> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _producer = producer;
        _kafkaOptions = kafkaOptions;
        _publishOptions = publishOptions;
        _state = state;
        _control = control;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topic = _kafkaOptions.Value.OutboxTopic;
        var batchSize = Math.Clamp(_publishOptions.Value.BatchSize, 1, 500);
        var delay = TimeSpan.FromSeconds(Math.Clamp(_publishOptions.Value.PollIntervalSeconds, 1, 300));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(topic, batchSize, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Outbox publish iteration failed");
                _state.RecordBatch(0, DateTimeOffset.UtcNow, null, ex.Message);
            }

            if (_control.ConsumeWakeRequest())
                continue;

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task PublishBatchAsync(string topic, int batchSize, CancellationToken cancellationToken)
    {
        var cs = _connectionStringProvider.GetConnectionString();
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync(cancellationToken);

        var published = 0;
        KafkaDelivery? lastDelivery = null;

        for (var i = 0; i < batchSize; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            OutboxRow row;
            await using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    SELECT id, aggregate_type, aggregate_id, event_type, payload, created_at_utc
                    FROM outbox_messages
                    WHERE processed_at_utc IS NULL
                    ORDER BY id
                    LIMIT 1
                    FOR UPDATE SKIP LOCKED
                    """;
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    await reader.CloseAsync();
                    await tx.CommitAsync(cancellationToken);
                    _state.RecordBatch(published, DateTimeOffset.UtcNow, lastDelivery?.At, null);
                    return;
                }

                row = new OutboxRow(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetGuid(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetDateTime(5));
                await reader.CloseAsync();
            }

            var envelopeJson = BuildEnvelopeJson(row);
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = row.AggregateId.ToString(),
                    Value = envelopeJson
                },
                cancellationToken);

            lastDelivery = new KafkaDelivery(DateTimeOffset.UtcNow, result.Offset.Value);

            await using (var update = conn.CreateCommand())
            {
                update.Transaction = tx;
                update.CommandText = "UPDATE outbox_messages SET processed_at_utc = @p WHERE id = @id";
                update.Parameters.AddWithValue("p", DateTime.UtcNow);
                update.Parameters.AddWithValue("id", row.Id);
                var updated = await update.ExecuteNonQueryAsync(cancellationToken);
                if (updated != 1)
                    throw new InvalidOperationException($"Expected 1 outbox row updated, got {updated}");
            }

            await tx.CommitAsync(cancellationToken);
            published++;
        }

        _state.RecordBatch(published, DateTimeOffset.UtcNow, lastDelivery?.At, null);
    }

    private static string BuildEnvelopeJson(OutboxRow row)
    {
        var payloadElement = JsonSerializer.Deserialize<JsonElement>(row.Payload);
        return JsonSerializer.Serialize(new
        {
            outboxId = row.Id,
            aggregateType = row.AggregateType,
            aggregateId = row.AggregateId,
            eventType = row.EventType,
            payload = payloadElement,
            createdAtUtc = row.CreatedAtUtc
        });
    }

    private readonly record struct OutboxRow(
        long Id,
        string AggregateType,
        Guid AggregateId,
        string EventType,
        string Payload,
        DateTime CreatedAtUtc);

    private readonly record struct KafkaDelivery(DateTimeOffset At, long Offset);
}
