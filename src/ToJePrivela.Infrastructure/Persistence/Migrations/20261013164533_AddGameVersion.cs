using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Games");
        }
    }
}
