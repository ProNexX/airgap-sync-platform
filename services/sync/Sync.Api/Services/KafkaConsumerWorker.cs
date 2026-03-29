using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Sync.Api.Options;

namespace Sync.Api.Services;

public sealed class KafkaConsumerWorker : BackgroundService
{
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly ConsumerState _state;
    private readonly ILogger<KafkaConsumerWorker> _logger;

    public KafkaConsumerWorker(
        IOptions<KafkaOptions> kafkaOptions,
        ConsumerState state,
        ILogger<KafkaConsumerWorker> logger)
    {
        _kafkaOptions = kafkaOptions;
        _state = state;
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
            EnableAutoCommit = true
        }).SetErrorHandler((_, e) =>
        {
            if (e.IsFatal)
                _logger.LogError("Kafka fatal error: {Reason}", e.Reason);
            else
                _logger.LogWarning("Kafka error: {Reason}", e.Reason);
        }).Build();

        consumer.Subscribe(opts.OutboxTopic);
        _logger.LogInformation(
            "Subscribed as group {GroupId} to topic {Topic}",
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

                    _logger.LogDebug(
                        "Consumed {Topic} p={Partition} o={Offset}",
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
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
}
