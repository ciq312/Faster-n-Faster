using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FasterNFaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comment_thresholds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_wpm = table.Column<double>(type: "double precision", nullable: false),
                    max_wpm = table.Column<double>(type: "double precision", nullable: false),
                    comment_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cooldown_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comment_thresholds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lobbies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_mode = table.Column<string>(type: "text", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    invite_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    host_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lobbies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "lobby_players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lobby_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    join_order = table.Column<int>(type: "integer", nullable: false),
                    connection_id = table.Column<string>(type: "text", nullable: false),
                    is_connected = table.Column<bool>(type: "boolean", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lobby_players", x => x.id);
                    table.ForeignKey(
                        name: "FK_lobby_players_lobbies_lobby_id",
                        column: x => x.lobby_id,
                        principalTable: "lobbies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "race_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lobby_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lobby_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_wpm = table.Column<double>(type: "double precision", nullable: false),
                    net_wpm = table.Column<double>(type: "double precision", nullable: false),
                    accuracy = table.Column<double>(type: "double precision", nullable: false),
                    mistake_count = table.Column<int>(type: "integer", nullable: false),
                    finish_position = table.Column<int>(type: "integer", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_race_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_race_results_lobbies_lobby_id",
                        column: x => x.lobby_id,
                        principalTable: "lobbies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_race_results_lobby_players_lobby_player_id",
                        column: x => x.lobby_player_id,
                        principalTable: "lobby_players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lobbies_invite_code",
                table: "lobbies",
                column: "invite_code",
                unique: true,
                filter: "\"invite_code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_lobby_players_lobby_id",
                table: "lobby_players",
                column: "lobby_id");

            migrationBuilder.CreateIndex(
                name: "IX_race_results_lobby_id",
                table: "race_results",
                column: "lobby_id");

            migrationBuilder.CreateIndex(
                name: "IX_race_results_lobby_player_id",
                table: "race_results",
                column: "lobby_player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comment_thresholds");

            migrationBuilder.DropTable(
                name: "race_results");

            migrationBuilder.DropTable(
                name: "lobby_players");

            migrationBuilder.DropTable(
                name: "lobbies");
        }
    }
}
