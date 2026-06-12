using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProtocolService.Migrations
{
    public partial class AddCreatedAtToProtocols : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Protocols",
                type: "datetime2(0)",
                precision: 0,
                nullable: false,
                defaultValueSql: "GETUTCDATE()");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Protocols");
        }
    }
}
