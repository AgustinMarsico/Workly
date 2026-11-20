using Domain;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class WorklyDbContext : DbContext
{
    public WorklyDbContext(DbContextOptions<WorklyDbContext> options)
        : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Website).HasMaxLength(768).IsRequired();
            entity.HasIndex(c => c.Website).IsUnique();
            entity.Property(c => c.Base64Logo).HasColumnType("longtext").IsRequired();
            entity.OwnsOne(c => c.Address, a =>
            {
                a.Property(ad => ad.Street).HasMaxLength(200).IsRequired();
                a.Property(ad => ad.City).HasMaxLength(100).IsRequired();
                a.Property(ad => ad.State).HasMaxLength(100).IsRequired();
                a.Property(ad => ad.ZipCode).HasMaxLength(20).IsRequired();
                a.Property(ad => ad.Country).HasMaxLength(100).IsRequired();
            });
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("areas");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.CompanyId).IsRequired();

            entity.HasOne<Company>()
                .WithMany(c => c.Areas)
                .HasForeignKey(a => a.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

    }
}
