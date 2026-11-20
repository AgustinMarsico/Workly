using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WorklyReservationService.DataAccess.Migrations;

[Migration("20260615000000_InitialReservationsOnly")]
[DbContext(typeof(WorklyReservationServiceDbContext))]
public partial class InitialReservationsOnly : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "reservations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                RoomId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                StartUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                EndUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CancelledAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                CancelledByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reservations", x => x.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_reservations_CompanyId",
            table: "reservations",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_reservations_RoomId",
            table: "reservations",
            column: "RoomId");

        migrationBuilder.CreateIndex(
            name: "IX_reservations_RoomId_StartUtc_EndUtc",
            table: "reservations",
            columns: new[] { "RoomId", "StartUtc", "EndUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_reservations_UserId",
            table: "reservations",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "reservations");
    }
}
