using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackDoubles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Doubles",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Doubles",
                table: "GamePlayers");
        }
    }
}
