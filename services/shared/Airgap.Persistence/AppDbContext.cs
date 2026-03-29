using Airgap.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Airgap.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataRecord> DataRecords => Set<DataRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataRecord>(entity =>
        {
            entity.HasIndex(e => e.ClientRequestId)
                .IsUnique()
                .HasFilter("client_request_id IS NOT NULL");
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable(OutboxSchema.TableName);
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.ProcessedAtUtc, e.CreatedAtUtc });
        });
    }
}
