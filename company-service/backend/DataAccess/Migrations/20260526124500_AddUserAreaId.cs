using System;
using DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    [DbContext(typeof(WorklyDbContext))]
    [Migration("20260526124500_AddUserAreaId")]
    public partial class AddUserAreaId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AreaId",
                table: "users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_users_AreaId",
                table: "users",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_areas_AreaId",
                table: "users",
                column: "AreaId",
                principalTable: "areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_areas_AreaId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_AreaId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "users");
        }
    }
}