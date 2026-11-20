using DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(WorklyDbContext))]
    [Migration("20260526120000_AddAreasAndInvitationAreaId")]
    public partial class AddAreasAndInvitationAreaId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CompanyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_areas_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_areas_CompanyId",
                table: "areas",
                column: "CompanyId");

            migrationBuilder.AddColumn<Guid>(
                name: "AreaId",
                table: "Invitations",
                type: "char(36)",
                nullable: false,
                defaultValue: Guid.Empty,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_AreaId",
                table: "Invitations",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CompanyId",
                table: "Invitations",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_areas_AreaId",
                table: "Invitations",
                column: "AreaId",
                principalTable: "areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_companies_CompanyId",
                table: "Invitations",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_areas_AreaId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_companies_CompanyId",
                table: "Invitations");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_AreaId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_CompanyId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Invitations");
        }
    }
}