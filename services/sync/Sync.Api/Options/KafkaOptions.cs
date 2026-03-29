namespace Sync.Api.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = default!;
    public string OutboxTopic { get; set; } = "airgap.outbox";
    public string GroupId { get; set; } = "sync-service";
}
