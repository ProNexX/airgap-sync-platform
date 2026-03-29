using System.Text.Json;
using Airgap.Persistence;
using Airgap.Persistence.Entities;
using Data.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Data.Api.Services;

public sealed class DataWriteService
{
    private const string DataRecordCreatedEvent = "DataRecordCreated";

    private readonly AppDbContext _db;

    public DataWriteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DataRecordDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.DataRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<DataRecordDto> CreateAsync(CreateDataRequest request, CancellationToken cancellationToken)
    {
        if (request.ClientRequestId is { } clientId)
        {
            var existing = await _db.DataRecords.AsNoTracking()
                .FirstOrDefaultAsync(r => r.ClientRequestId == clientId, cancellationToken);
            if (existing is not null)
                return ToDto(existing);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var entity = new DataRecord
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Value = request.Value,
                ClientRequestId = request.ClientRequestId,
                CreatedAtUtc = now
            };

            _db.DataRecords.Add(entity);

            var payload = JsonSerializer.Serialize(new
            {
                entity.Id,
                entity.Name,
                entity.Value,
                entity.ClientRequestId,
                entity.CreatedAtUtc
            });

            _db.OutboxMessages.Add(new OutboxMessage
            {
                AggregateType = nameof(DataRecord),
                AggregateId = entity.Id,
                EventType = DataRecordCreatedEvent,
                Payload = payload,
                CreatedAtUtc = now
            });

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToDto(entity);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static DataRecordDto ToDto(DataRecord entity) =>
        new(entity.Id, entity.Name, entity.Value, entity.ClientRequestId, entity.CreatedAtUtc);
}
