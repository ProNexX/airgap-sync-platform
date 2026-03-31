namespace Telemetry.Api.Models;

public sealed class StreamedSensorReading
{
    public string DeviceId { get; init; } = default!;
    public string? Metric { get; init; }
    public double? Value { get; init; }
    public string? Unit { get; init; }
    public DateTimeOffset? RecordedAt { get; init; }
    public string EventType { get; init; } = default!;
    public long? OutboxId { get; init; }
    public Guid? RecordId { get; init; }
    public string? RawPayloadJson { get; init; }
}
