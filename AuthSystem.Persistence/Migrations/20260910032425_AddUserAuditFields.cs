using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Users");
        }
    }
}
