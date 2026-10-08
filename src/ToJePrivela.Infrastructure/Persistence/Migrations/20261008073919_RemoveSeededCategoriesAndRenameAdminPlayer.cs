using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededCategoriesAndRenameAdminPlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The seeded categories go only while they hold no questions: a fresh database starts without them, and an
            // existing one never loses a question (deleting a category cascades to its questions).
            migrationBuilder.Sql("""
                DELETE FROM "QuestionCategories"
                WHERE "Id" IN (1, 2, 3)
                  AND NOT EXISTS (SELECT 1 FROM "Questions" WHERE "Questions"."CategoryId" = "QuestionCategories"."Id");
                """);

            // Only a player still called Admin is renamed, and only when its login has no Peter yet (unique name index).
            migrationBuilder.Sql("""
                UPDATE "Players" SET "Name" = 'Peter', "NameKey" = 'peter'
                WHERE "Id" = 1
                  AND "NameKey" = 'admin'
                  AND NOT EXISTS (
                      SELECT 1 FROM "Players" AS "Other"
                      WHERE "Other"."AccountId" = "Players"."AccountId" AND "Other"."NameKey" = 'peter');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Players" SET "Name" = 'Admin', "NameKey" = 'admin'
                WHERE "Id" = 1
                  AND "NameKey" = 'peter'
                  AND NOT EXISTS (
                      SELECT 1 FROM "Players" AS "Other"
                      WHERE "Other"."AccountId" = "Players"."AccountId" AND "Other"."NameKey" = 'admin');
                """);

            migrationBuilder.Sql("""
                INSERT OR IGNORE INTO "QuestionCategories" ("Id", "Name", "NameKey") VALUES
                    (1, 'Cars', 'cars'),
                    (2, 'Sport', 'sport'),
                    (3, 'History', 'history');
                """);
        }
    }
}
