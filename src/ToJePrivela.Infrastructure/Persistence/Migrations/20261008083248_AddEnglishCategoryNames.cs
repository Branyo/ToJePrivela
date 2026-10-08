using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishCategoryNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NameKey",
                table: "QuestionCategories",
                newName: "NameSkKey");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "QuestionCategories",
                newName: "NameSk");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionCategories_NameKey",
                table: "QuestionCategories",
                newName: "IX_QuestionCategories_NameSkKey");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "QuestionCategories",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                collation: "NOCASE");

            migrationBuilder.AddColumn<string>(
                name: "NameEnKey",
                table: "QuestionCategories",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // The categories in use get their English names; any other category starts with its Slovak name, which an
            // admin can replace by deleting the category and creating it again.
            migrationBuilder.Sql("""
                UPDATE "QuestionCategories" SET "NameEn" = CASE "NameSkKey"
                        WHEN 'hračky' THEN 'Toys'
                        WHEN 'herci bojových umení' THEN 'Martial arts actors'
                        WHEN 'vtáky' THEN 'Birds'
                        WHEN 'vlaky' THEN 'Trains'
                        WHEN 'hollywood' THEN 'Hollywood'
                        ELSE "NameSk"
                    END;
                """);

            // NameKeys.Of: the English names above are ASCII, and every other key is the Slovak one.
            migrationBuilder.Sql("""
                UPDATE "QuestionCategories" SET "NameEnKey" = CASE
                        WHEN "NameEn" = "NameSk" THEN "NameSkKey"
                        ELSE lower("NameEn")
                    END;
                """);

            // The English key is unique too; a name some other category already took gets its id appended.
            migrationBuilder.Sql("""
                UPDATE "QuestionCategories"
                SET "NameEn" = "NameEn" || ' #' || "Id", "NameEnKey" = "NameEnKey" || ' #' || "Id"
                WHERE EXISTS (
                    SELECT 1 FROM "QuestionCategories" AS "Other"
                    WHERE "Other"."NameEnKey" = "QuestionCategories"."NameEnKey" AND "Other"."Id" < "QuestionCategories"."Id");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_NameEnKey",
                table: "QuestionCategories",
                column: "NameEnKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionCategories_NameEnKey",
                table: "QuestionCategories");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "QuestionCategories");

            migrationBuilder.DropColumn(
                name: "NameEnKey",
                table: "QuestionCategories");

            migrationBuilder.RenameColumn(
                name: "NameSkKey",
                table: "QuestionCategories",
                newName: "NameKey");

            migrationBuilder.RenameColumn(
                name: "NameSk",
                table: "QuestionCategories",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_QuestionCategories_NameSkKey",
                table: "QuestionCategories",
                newName: "IX_QuestionCategories_NameKey");
        }
    }
}
