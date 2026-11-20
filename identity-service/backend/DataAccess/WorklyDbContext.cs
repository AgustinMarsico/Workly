using Domain;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class WorklyDbContext : DbContext
{
    public WorklyDbContext(DbContextOptions<WorklyDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<UserNotificationPreference> UserNotificationPreferences => Set<UserNotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .IsRequired();

            entity.Property(u => u.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(u => u.CompanyId)
                .IsRequired(false);

            entity.Property(u => u.AreaId)
                .IsRequired(false);

            entity.HasIndex(u => u.CompanyId);
            entity.HasIndex(u => u.AreaId);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");
            entity.HasKey(k => k.Id);

            entity.Property(k => k.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(k => k.KeyHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(k => k.KeyHash)
                .IsUnique();

            entity.Property(k => k.IsActive)
                .IsRequired();

            entity.Property(k => k.CreatedAt)
                .IsRequired();

            entity.HasOne(k => k.User)
                .WithMany()
                .HasForeignKey(k => k.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.UserEmail)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(i => i.CompanyId)
                .IsRequired();

            entity.Property(i => i.AreaId)
                .IsRequired();

            entity.Property(i => i.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(i => i.CreatedById)
                .IsRequired();

            entity.Property(i => i.InvitationStatus)
                .HasMaxLength(32)
                .HasConversion<string>()
                .IsRequired();

            entity.HasIndex(i => i.CompanyId);
            entity.HasIndex(i => i.AreaId);
            entity.HasIndex(i => i.TokenHash);
        });

        modelBuilder.Entity<UserNotificationPreference>(entity =>
        {
            entity.ToTable("user_notification_preferences");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.UserId)
                .IsRequired();

            entity.HasIndex(u => u.UserId)
                .IsUnique();

            entity.Property(u => u.EmailNotificationsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(u => u.InAppNotificationsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                .IsRequired();

            entity.Property(u => u.UpdatedAt)
                .IsRequired(false);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
