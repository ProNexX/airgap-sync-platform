namespace Data.Api.Options;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = default!;
    public string OutboxTopic { get; set; } = "airgap.outbox";
}
