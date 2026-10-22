using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreLastCaller : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastCallerId",
                table: "Games",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCallerId",
                table: "Games");
        }
    }
}
