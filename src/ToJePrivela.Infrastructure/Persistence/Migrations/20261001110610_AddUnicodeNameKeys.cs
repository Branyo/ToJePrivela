using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnicodeNameKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionCategories_Name",
                table: "QuestionCategories");

            migrationBuilder.DropIndex(
                name: "IX_Players_Name",
                table: "Players");

            migrationBuilder.AddColumn<string>(
                name: "NameKey",
                table: "QuestionCategories",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameKey",
                table: "Players",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 1,
                column: "NameKey",
                value: "admin");

            migrationBuilder.UpdateData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 2,
                column: "NameKey",
                value: "brano");

            migrationBuilder.UpdateData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 3,
                column: "NameKey",
                value: "duri");

            migrationBuilder.UpdateData(
                table: "QuestionCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "NameKey",
                value: "cars");

            migrationBuilder.UpdateData(
                table: "QuestionCategories",
                keyColumn: "Id",
                keyValue: 2,
                column: "NameKey",
                value: "sport");

            migrationBuilder.UpdateData(
                table: "QuestionCategories",
                keyColumn: "Id",
                keyValue: 3,
                column: "NameKey",
                value: "history");

            // The seeded keys above are only right while the seeded names are; every row gets its key from its
            // stored name instead. Rows whose names now collide ("Štefan" and "štefan") keep the oldest as it is
            // and get " #<id>" appended, so the unique indexes below can be built.
            foreach (var table in new[] { "Players", "QuestionCategories" })
            {
                migrationBuilder.Sql($"UPDATE \"{table}\" SET \"NameKey\" = {NameKeyOf("\"Name\"")};");
                migrationBuilder.Sql($"""
                    UPDATE "{table}"
                    SET "Name" = "Name" || ' #' || "Id", "NameKey" = "NameKey" || ' #' || "Id"
                    WHERE "Id" NOT IN (SELECT MIN("Id") FROM "{table}" GROUP BY "NameKey");
                    """);
            }

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_NameKey",
                table: "QuestionCategories",
                column: "NameKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_NameKey",
                table: "Players",
                column: "NameKey",
                unique: true);
        }

        /// <summary>
        /// SQL for <c>NameKeys.Of</c> on names already stored: SQLite's lower() folds ASCII only, so every upper-case
        /// letter of Latin-1 and Latin Extended-A (Slovak, Czech, Hungarian, Polish, German, ...) is replaced first.
        /// Computed here rather than read from the domain, so later changes there do not alter this migration.
        /// </summary>
        private static string NameKeyOf(string column)
        {
            var sql = $"trim({column})";

            for (var code = 0xC0; code <= 0x17F; code++)
            {
                var upper = (char)code;
                var lower = char.ToLowerInvariant(upper);

                if (lower != upper)
                {
                    sql = $"replace({sql}, '{upper}', '{lower}')";
                }
            }

            return $"lower({sql})";
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuestionCategories_NameKey",
                table: "QuestionCategories");

            migrationBuilder.DropIndex(
                name: "IX_Players_NameKey",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "NameKey",
                table: "QuestionCategories");

            migrationBuilder.DropColumn(
                name: "NameKey",
                table: "Players");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_Name",
                table: "QuestionCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Name",
                table: "Players",
                column: "Name",
                unique: true);
        }
    }
}
