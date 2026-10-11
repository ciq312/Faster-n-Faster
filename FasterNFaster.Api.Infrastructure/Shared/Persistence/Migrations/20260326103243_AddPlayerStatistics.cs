using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FasterNFaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    RacesTyped = table.Column<int>(type: "integer", nullable: false),
                    BestWPM = table.Column<float>(type: "real", nullable: false),
                    SumWPM = table.Column<float>(type: "real", nullable: false),
                    AvgWPM = table.Column<float>(type: "real", nullable: false),
                    BestAccuracy = table.Column<float>(type: "real", nullable: false),
                    SumAccuracy = table.Column<float>(type: "real", nullable: false),
                    AvgAccuracy = table.Column<float>(type: "real", nullable: false),
                    SymbolsTyped = table.Column<int>(type: "integer", nullable: false),
                    WordsTyped = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Statistics_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Statistics");
        }
    }
}
