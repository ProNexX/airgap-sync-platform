using System.ComponentModel.DataAnnotations;

namespace Gateway.Api.Models;

public sealed class SensorTelemetryRequest
{
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = default!;

    [Required]
    [MaxLength(64)]
    public string Metric { get; set; } = default!;

    [Required]
    public double Value { get; set; }

    [MaxLength(32)]
    public string? Unit { get; set; }

    public DateTimeOffset? RecordedAt { get; set; }

    public Guid? ClientRequestId { get; set; }
}
