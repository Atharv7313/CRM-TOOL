using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProtocolService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Protocols",
                columns: table => new
                {
                    ProtocolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "UPCOMING"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2(0)", precision: 0, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Protocols", x => x.ProtocolId);
                    table.ForeignKey(
                        name: "FK_Protocols_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.SiteId);
                });

            migrationBuilder.CreateTable(
                name: "ProtocolSites",
                columns: table => new
                {
                    ProtocolSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ProtocolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestigatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ACTIVE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProtocolSites", x => x.ProtocolSiteId);
                    table.ForeignKey(
                        name: "FK_ProtocolSites_Protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "Protocols",
                        principalColumn: "ProtocolId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProtocolSites_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "SiteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProtocolSites_Users_InvestigatorUserId",
                        column: x => x.InvestigatorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_CreatedBy",
                table: "Protocols",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Protocols_Status",
                table: "Protocols",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolSites_InvestigatorUserId",
                table: "ProtocolSites",
                column: "InvestigatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolSites_Protocol",
                table: "ProtocolSites",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_ProtocolSites_Site",
                table: "ProtocolSites",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProtocolSites",
                table: "ProtocolSites",
                columns: new[] { "ProtocolId", "SiteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProtocolSites");

            migrationBuilder.DropTable(
                name: "Protocols");

            migrationBuilder.DropTable(
                name: "Sites");
        }
    }
}
