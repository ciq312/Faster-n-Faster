using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FasterNFaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class BannedPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BannedPlayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedPlayers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannedPlayers_UserId",
                table: "BannedPlayers",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedPlayers");
        }
    }
}
