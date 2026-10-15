using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipientId = table.Column<int>(type: "INTEGER", nullable: false),
                    CallerId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    BadPoints = table.Column<int>(type: "INTEGER", nullable: false),
                    AwardedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameCards_GamePlayers_CallerId",
                        column: x => x.CallerId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameCards_GamePlayers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "GamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameCards_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameCards_QuestionCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameCards_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameCards_CallerId",
                table: "GameCards",
                column: "CallerId");

            migrationBuilder.CreateIndex(
                name: "IX_GameCards_CategoryId",
                table: "GameCards",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_GameCards_GameId",
                table: "GameCards",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_GameCards_QuestionId",
                table: "GameCards",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameCards_RecipientId",
                table: "GameCards",
                column: "RecipientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameCards");
        }
    }
}
