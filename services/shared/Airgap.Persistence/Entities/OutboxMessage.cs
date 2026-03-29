namespace Airgap.Persistence.Entities;

public sealed class OutboxMessage
{
    public long Id { get; set; }
    public string AggregateType { get; set; } = default!;
    public Guid AggregateId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
