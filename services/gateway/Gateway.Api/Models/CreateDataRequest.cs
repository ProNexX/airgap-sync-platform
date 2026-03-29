using System.ComponentModel.DataAnnotations;

namespace Gateway.Api.Models;

public class CreateDataRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required]
    [MaxLength(1000)]
    public string Value { get; set; } = default!;

    public Guid? ClientRequestId { get; set; }
}
