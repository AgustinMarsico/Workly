using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScopeRoomsToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "rooms",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_rooms_CompanyId",
                table: "rooms",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_rooms_companies_CompanyId",
                table: "rooms",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rooms_companies_CompanyId",
                table: "rooms");

            migrationBuilder.DropIndex(
                name: "IX_rooms_CompanyId",
                table: "rooms");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "rooms");
        }
    }
}
