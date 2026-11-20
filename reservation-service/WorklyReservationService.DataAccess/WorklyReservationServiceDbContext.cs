using Microsoft.EntityFrameworkCore;
using WorklyReservationService.Domain;

namespace WorklyReservationService.DataAccess;

public class WorklyReservationServiceDbContext : DbContext
{
    public WorklyReservationServiceDbContext(DbContextOptions<WorklyReservationServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<AreaNotificationRecipient> AreaNotificationRecipients => Set<AreaNotificationRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("reservations");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CompanyId).IsRequired();
            entity.Property(r => r.StartUtc).IsRequired();
            entity.Property(r => r.EndUtc).IsRequired();
            entity.Property(r => r.CreatedAtUtc).IsRequired();
            entity.Property(r => r.CancelledAtUtc).IsRequired(false);
            entity.Property(r => r.CancelledByUserId).IsRequired(false);
            entity.HasIndex(r => r.RoomId);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.CompanyId);
            entity.HasIndex(r => new { r.RoomId, r.StartUtc, r.EndUtc });
        });

        modelBuilder.Entity<AreaNotificationRecipient>(entity =>
        {
            entity.ToTable("area_notification_recipients");
            entity.HasKey(r => new { r.AreaId, r.UserId });
            entity.Property(r => r.Email).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Name).HasMaxLength(200);
            entity.Property(r => r.EmailSubscribed).IsRequired();
            entity.Property(r => r.UpdatedAtUtc).IsRequired();
            entity.HasIndex(r => r.AreaId);
        });
    }
}
