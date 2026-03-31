using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Telemetry.Api.Hubs;
using Telemetry.Api.Options;

namespace Telemetry.Api.Services;

public sealed class KafkaConsumerWorker : BackgroundService
{
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly ConsumerState _state;
    private readonly IHubContext<TelemetryHub> _hub;
    private readonly ILogger<KafkaConsumerWorker> _logger;

    public KafkaConsumerWorker(
        IOptions<KafkaOptions> kafkaOptions,
        ConsumerState state,
        IHubContext<TelemetryHub> hub,
        ILogger<KafkaConsumerWorker> logger)
    {
        _kafkaOptions = kafkaOptions;
        _state = state;
        _hub = hub;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(
            () => Run(stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private void Run(CancellationToken stoppingToken)
    {
        var opts = _kafkaOptions.Value;
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = opts.BootstrapServers,
            GroupId = opts.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = true
        }).SetErrorHandler((_, e) =>
        {
            if (e.IsFatal)
                _logger.LogError("Kafka fatal error: {Reason}", e.Reason);
            else
                _logger.LogWarning("Kafka error: {Reason}", e.Reason);
        }).Build();

        EnsureTopicExists(opts.BootstrapServers, opts.OutboxTopic, stoppingToken);

        consumer.Subscribe(opts.OutboxTopic);
        _logger.LogInformation(
            "Telemetry ingest subscribed (group {GroupId}) to {Topic}",
            opts.GroupId,
            opts.OutboxTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(750));
                    if (result is null)
                        continue;
                    if (result.IsPartitionEOF)
                        continue;

                    var msg = result.Message;
                    _state.RecordConsumed(
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value,
                        msg.Key);

                    if (!string.IsNullOrEmpty(msg.Value))
                    {
                        var reading = TelemetryEnvelopeParser.TryParseOutboxEnvelope(msg.Value);
                        if (reading is not null)
                        {
                            _hub.Clients.All.SendAsync("SensorReading", reading, stoppingToken)
                                .GetAwaiter()
                                .GetResult();
                        }
                    }

                    _logger.LogDebug(
                        "Consumed {Topic} p={Partition} o={Offset}",
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
                }
                catch (ConsumeException ex) when (IsTopicNotYetAvailable(ex))
                {
                    _logger.LogWarning(
                        "Topic not available yet ({Code}: {Reason}), retrying shortly",
                        ex.Error.Code,
                        ex.Error.Reason);
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume failed");
                    _state.RecordError(ex.Error.Reason);
                }
            }
        }
        finally
        {
            try
            {
                consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer");
            }
        }
    }

    private void EnsureTopicExists(string bootstrapServers, string topic, CancellationToken stoppingToken)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();

        try
        {
            admin.CreateTopicsAsync(
                    [
                        new TopicSpecification
                        {
                            Name = topic,
                            NumPartitions = 1,
                            ReplicationFactor = 1
                        }
                    ],
                    new CreateTopicsOptions { RequestTimeout = TimeSpan.FromSeconds(30) })
                .GetAwaiter()
                .GetResult();
            _logger.LogInformation("Created Kafka topic {Topic}", topic);
        }
        catch (CreateTopicsException ex)
        {
            var fatal = false;
            foreach (var r in ex.Results)
            {
                if (r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists)
                    continue;
                fatal = true;
                _logger.LogError("Kafka create topic {Topic} failed: {Reason}", topic, r.Error.Reason);
            }

            if (fatal)
                throw;
        }

        WaitUntilTopicInMetadata(admin, topic, stoppingToken);

        stoppingToken.ThrowIfCancellationRequested();
    }

    private static bool IsTopicNotYetAvailable(ConsumeException ex) =>
        ex.Error.Code is ErrorCode.UnknownTopicOrPart
            or ErrorCode.Local_UnknownTopic
            or ErrorCode.Local_UnknownPartition;

    private void WaitUntilTopicInMetadata(IAdminClient admin, string topic, CancellationToken stoppingToken)
    {
        for (var i = 0; i < 120; i++)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var md = admin.GetMetadata(topic, TimeSpan.FromSeconds(10));
            var t = md.Topics.FirstOrDefault(x => x.Topic == topic);
            if (t is not null
                && t.Partitions.Count > 0
                && t.Error.Code == ErrorCode.NoError)
            {
                if (i > 0)
                    _logger.LogInformation("Kafka topic {Topic} visible in metadata after {Attempts} attempt(s)", topic, i);
                return;
            }

            Thread.Sleep(250);
        }

        _logger.LogWarning(
            "Kafka topic {Topic} still not in broker metadata; consume loop will retry on topic-unavailable errors",
            topic);
    }
}
