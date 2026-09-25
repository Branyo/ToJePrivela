using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces the free-text Questions.Category with a CategoryId foreign key (cascading on delete),
    /// replaces Difficulty with stored BadPoints, and adds Source and CreatedAt.
    /// Hand-edited so existing questions survive: missing categories are created, bad points keep
    /// their former value (6 - difficulty) and every existing question counts as manual.
    /// </summary>
    public partial class LinkQuestionsToCategoriesAndTrackSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BadPoints",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Questions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Questions",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            // Questions.Category carries the NOCASE collation, so GROUP BY folds "sport" into "Sport"
            // and the comparison with QuestionCategories.Name (also NOCASE) ignores case as well.
            migrationBuilder.Sql("""
                INSERT INTO "QuestionCategories" ("Name")
                SELECT "Category" FROM "Questions" AS q
                WHERE NOT EXISTS (SELECT 1 FROM "QuestionCategories" AS c WHERE c."Name" = q."Category")
                GROUP BY "Category";
                """);

            migrationBuilder.Sql("""
                UPDATE "Questions" SET
                    "CategoryId" = (SELECT c."Id" FROM "QuestionCategories" AS c WHERE c."Name" = "Questions"."Category"),
                    "BadPoints" = 6 - "Difficulty",
                    "Source" = 'Manual',
                    "CreatedAt" = strftime('%Y-%m-%d %H:%M:%f', 'now');
                """);

            migrationBuilder.DropIndex(
                name: "IX_Questions_Category",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "Questions");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CategoryId_Source",
                table: "Questions",
                columns: new[] { "CategoryId", "Source" });

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionCategories_CategoryId",
                table: "Questions",
                column: "CategoryId",
                principalTable: "QuestionCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionCategories_CategoryId",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_Questions_CategoryId_Source",
                table: "Questions");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Questions",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE "Questions" SET
                    "Category" = (SELECT c."Name" FROM "QuestionCategories" AS c WHERE c."Id" = "Questions"."CategoryId"),
                    "Difficulty" = 6 - "BadPoints";
                """);

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "BadPoints",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Questions");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_Category",
                table: "Questions",
                column: "Category");
        }
    }
}
