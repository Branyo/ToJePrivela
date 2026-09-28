using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignPlayerAvatars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Players",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            // Players that already exist (the seeded ones too) get a random avatar; random() is evaluated per row.
            var cases = string.Join(" ", ExistingPlayerAvatars.Select((avatar, index) => $"WHEN {index} THEN '{avatar}'"));
            migrationBuilder.Sql(
                $"UPDATE \"Players\" SET \"Avatar\" = CASE abs(random()) % {ExistingPlayerAvatars.Length} {cases} END;");
        }

        /// <summary>A copy of the avatar pool at the time of this migration, so later pool changes do not alter it.</summary>
        private static readonly string[] ExistingPlayerAvatars =
        [
            "🦊", "🐸", "🐼", "🐯", "🐙", "🦄", "🐨", "🐧", "🦁", "🐵", "🐰", "🦉",
            "🐶", "🐱", "🐻", "🐷", "🐮", "🐔", "🦋", "🐢", "🦀", "🐳", "🦒", "🦔",
        ];

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Players");
        }
    }
}
