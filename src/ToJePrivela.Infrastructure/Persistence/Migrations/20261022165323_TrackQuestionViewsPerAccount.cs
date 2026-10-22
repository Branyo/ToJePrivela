using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackQuestionViewsPerAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionViews",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuestionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    LastViewedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionViews", x => new { x.AccountId, x.QuestionId });
                    table.ForeignKey(
                        name: "FK_QuestionViews_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionViews_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionViews_QuestionId",
                table: "QuestionViews",
                column: "QuestionId");

            // Nobody knows which login saw what so far, so every login that can sign in starts from the shared counts it
            // played under: nobody gets a question again that the shared counts kept from them. From here on each login
            // counts its own views.
            migrationBuilder.Sql(
                """
                INSERT INTO "QuestionViews" ("AccountId", "QuestionId", "Count", "LastViewedAt")
                SELECT "a"."Id", "q"."Id", "q"."ViewCount", COALESCE("q"."LastViewedAt", "q"."CreatedAt")
                FROM "Accounts" AS "a"
                CROSS JOIN "Questions" AS "q"
                WHERE "a"."PasswordHash" IS NOT NULL AND "q"."ViewCount" > 0;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Questions_CategoryId_ViewCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LastViewedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Questions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewedAt",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CategoryId_ViewCount",
                table: "Questions",
                columns: new[] { "CategoryId", "ViewCount" });

            // The shared counts come back as the most any login saw each question.
            migrationBuilder.Sql(
                """
                UPDATE "Questions"
                SET "ViewCount" = COALESCE((SELECT MAX("v"."Count") FROM "QuestionViews" AS "v" WHERE "v"."QuestionId" = "Questions"."Id"), 0),
                    "LastViewedAt" = (SELECT MAX("v"."LastViewedAt") FROM "QuestionViews" AS "v" WHERE "v"."QuestionId" = "Questions"."Id");
                """);

            migrationBuilder.DropTable(
                name: "QuestionViews");
        }
    }
}
