namespace Airgap.Persistence.Entities;

public sealed class DataRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Value { get; set; } = default!;
    public Guid? ClientRequestId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
