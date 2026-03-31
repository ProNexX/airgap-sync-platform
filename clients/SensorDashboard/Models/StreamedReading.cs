namespace SensorDashboard.Models;

public sealed class StreamedReading
{
    public string DeviceId { get; set; } = "";
    public string? Metric { get; set; }
    public double? Value { get; set; }
    public string? Unit { get; set; }
    public DateTimeOffset? RecordedAt { get; set; }
    public string EventType { get; set; } = "";
    public long? OutboxId { get; set; }
    public Guid? RecordId { get; set; }
}
