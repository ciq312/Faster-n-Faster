using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FasterNFaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExpiresAtAndSuspensionCountToBannedPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "BannedPlayers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuspensionCount",
                table: "BannedPlayers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "BannedPlayers");

            migrationBuilder.DropColumn(
                name: "SuspensionCount",
                table: "BannedPlayers");
        }
    }
}
