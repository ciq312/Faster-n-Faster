using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FasterNFaster.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExternalLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Token",
                table: "Users",
                newName: "Email");

            migrationBuilder.CreateTable(
                name: "ExternalLogins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalSubject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExternalEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_Provider_ExternalSubject",
                table: "ExternalLogins",
                columns: new[] { "Provider", "ExternalSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalLogins_UserId",
                table: "ExternalLogins",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalLogins");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Users",
                newName: "Token");
        }
    }
}
