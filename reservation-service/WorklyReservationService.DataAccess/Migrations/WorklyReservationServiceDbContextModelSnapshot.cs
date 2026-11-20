using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace WorklyReservationService.DataAccess.Migrations;

[DbContext(typeof(WorklyReservationServiceDbContext))]
partial class WorklyReservationServiceDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.13")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("WorklyReservationService.Domain.Reservation", b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("char(36)");

            b.Property<DateTime?>("CancelledAtUtc")
                .HasColumnType("datetime(6)");

            b.Property<Guid?>("CancelledByUserId")
                .HasColumnType("char(36)");

            b.Property<Guid>("CompanyId")
                .HasColumnType("char(36)");

            b.Property<DateTime>("CreatedAtUtc")
                .HasColumnType("datetime(6)");

            b.Property<DateTime>("EndUtc")
                .HasColumnType("datetime(6)");

            b.Property<Guid>("RoomId")
                .HasColumnType("char(36)");

            b.Property<DateTime>("StartUtc")
                .HasColumnType("datetime(6)");

            b.Property<Guid>("UserId")
                .HasColumnType("char(36)");

            b.HasKey("Id");

            b.HasIndex("CompanyId");

            b.HasIndex("RoomId");
            b.HasIndex("RoomId", "StartUtc", "EndUtc");
            b.HasIndex("UserId");

            b.ToTable("reservations", (string)null);
        });

        modelBuilder.Entity("WorklyReservationService.Domain.AreaNotificationRecipient", b =>
        {
            b.Property<Guid>("AreaId")
                .HasColumnType("char(36)");

            b.Property<Guid>("UserId")
                .HasColumnType("char(36)");

            b.Property<string>("Email")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            b.Property<bool>("EmailSubscribed")
                .HasColumnType("tinyint(1)");

            b.Property<string>("Name")
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            b.Property<DateTime>("UpdatedAtUtc")
                .HasColumnType("datetime(6)");

            b.HasKey("AreaId", "UserId");

            b.HasIndex("AreaId");

            b.ToTable("area_notification_recipients", (string)null);
        });
#pragma warning restore 612, 618
    }
}
