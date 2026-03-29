namespace Data.Api.Models;

public sealed record DataRecordDto(
    Guid Id,
    string Name,
    string Value,
    Guid? ClientRequestId,
    DateTime CreatedAtUtc);
