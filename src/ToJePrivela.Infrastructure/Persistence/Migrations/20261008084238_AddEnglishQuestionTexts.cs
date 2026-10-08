using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishQuestionTexts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Text",
                table: "Questions",
                newName: "TextSk");

            // Stored questions keep their Slovak text only for now; their English texts come later.
            migrationBuilder.AddColumn<string>(
                name: "TextEn",
                table: "Questions",
                type: "TEXT",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextEn",
                table: "Questions");

            migrationBuilder.RenameColumn(
                name: "TextSk",
                table: "Questions",
                newName: "Text");
        }
    }
}
