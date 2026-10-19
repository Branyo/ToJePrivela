using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGamePlayerSeats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Seat",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // A game's seats were inserted in the order its players were added, so their ids give the order of play.
            migrationBuilder.Sql(
                """
                UPDATE GamePlayers
                SET Seat = (SELECT COUNT(*) FROM GamePlayers AS earlier
                            WHERE earlier.GameId = GamePlayers.GameId AND earlier.Id < GamePlayers.Id);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GameId_Seat",
                table: "GamePlayers",
                columns: new[] { "GameId", "Seat" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GamePlayers_GameId_Seat",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "Seat",
                table: "GamePlayers");
        }
    }
}
