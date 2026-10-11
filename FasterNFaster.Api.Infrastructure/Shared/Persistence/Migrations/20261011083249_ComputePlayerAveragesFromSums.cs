using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FasterNFaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class ComputePlayerAveragesFromSums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgAccuracy",
                table: "Statistics");

            migrationBuilder.DropColumn(
                name: "AvgWPM",
                table: "Statistics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "AvgAccuracy",
                table: "Statistics",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "AvgWPM",
                table: "Statistics",
                type: "real",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.Sql("""
                UPDATE "Statistics"
                SET "AvgWPM" = "SumWPM" / "RacesTyped",
                    "AvgAccuracy" = "SumAccuracy" / "RacesTyped"
                WHERE "RacesTyped" > 0;
                """);
        }
    }
}
