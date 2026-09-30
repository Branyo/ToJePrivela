using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBadPointsMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Games from before the choice keep their cards worth the question's stored bad points.
            migrationBuilder.AddColumn<string>(
                name: "BadPointsMode",
                table: "Games",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Question");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadPointsMode",
                table: "Games");
        }
    }
}
