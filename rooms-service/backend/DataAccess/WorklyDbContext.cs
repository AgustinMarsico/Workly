using Domain;
using Domain.RoomTypes;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class WorklyDbContext : DbContext
{
    public WorklyDbContext(DbContextOptions<WorklyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CompanyId).IsRequired();
            entity.HasIndex(r => r.CompanyId);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.Property(r => r.MaxCapacity).IsRequired();
            entity.Property(r => r.Equipment).HasMaxLength(2000);
            entity.Property(r => r.Type)
                .HasColumnName("type_key")
                .HasMaxLength(64)
                .IsRequired()
                .HasConversion(
                    v => v.Key,
                    v => RoomTypeFactory.GetRequired(v));
        });
    }
}
