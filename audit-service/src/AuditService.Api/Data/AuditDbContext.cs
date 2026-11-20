using AuditService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Api.Data;

public sealed class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public override int SaveChanges()
    {
        RejectAuditRecordMutations();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RejectAuditRecordMutations();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.ToTable("audit_records");
            entity.HasKey(record => record.Id);

            entity.Property(record => record.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(record => record.EventId).HasColumnName("event_id").IsRequired();
            entity.Property(record => record.OccurredAt).HasColumnName("occurred_at").IsRequired();
            entity.Property(record => record.ActionType).HasColumnName("action_type").HasMaxLength(100).IsRequired();
            entity.Property(record => record.UserId).HasColumnName("user_id");
            entity.Property(record => record.UserEmail).HasColumnName("user_email").HasMaxLength(320);
            entity.Property(record => record.CompanyId).HasColumnName("company_id");
            entity.Property(record => record.EntityType).HasColumnName("entity_type").HasMaxLength(100);
            entity.Property(record => record.EntityId).HasColumnName("entity_id").HasMaxLength(100);
            entity.Property(record => record.DetailsJson).HasColumnName("details_json").HasColumnType("nvarchar(max)");
            entity.Property(record => record.OldValuesJson).HasColumnName("old_values_json").HasColumnType("nvarchar(max)");
            entity.Property(record => record.NewValuesJson).HasColumnName("new_values_json").HasColumnType("nvarchar(max)");
            entity.Property(record => record.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
            entity.Property(record => record.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(record => record.EventId).IsUnique();
            entity.HasIndex(record => record.OccurredAt);
            entity.HasIndex(record => record.UserId);
            entity.HasIndex(record => record.CompanyId);
            entity.HasIndex(record => record.ActionType);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_audit_records_details_json_is_json", "details_json IS NULL OR ISJSON(details_json) = 1");
                table.HasCheckConstraint("CK_audit_records_old_values_json_is_json", "old_values_json IS NULL OR ISJSON(old_values_json) = 1");
                table.HasCheckConstraint("CK_audit_records_new_values_json_is_json", "new_values_json IS NULL OR ISJSON(new_values_json) = 1");
            });
        });
    }

    private void RejectAuditRecordMutations()
    {
        bool hasMutation = ChangeTracker
            .Entries<AuditRecord>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (hasMutation)
        {
            throw new InvalidOperationException("Audit records are immutable and cannot be updated or deleted.");
        }
    }
}
