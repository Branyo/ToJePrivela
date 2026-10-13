using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreGameLosers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLoser",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Games finished so far get their losers under the game's rule: everyone on the most final bad points, so
            // a tie is a shared loss whatever the cards; nobody lost a cancelled game or one without a card.
            migrationBuilder.Sql(
                """
                UPDATE "GamePlayers" AS me SET "IsLoser" = 1
                WHERE EXISTS (
                        SELECT 1 FROM "Games" AS g
                        WHERE g."Id" = me."GameId" AND g."Finished" IS NOT NULL AND g."IsCancelled" = 0)
                    AND EXISTS (
                        SELECT 1 FROM "GamePlayers" AS card WHERE card."GameId" = me."GameId" AND card."BadCards" > 0)
                    AND NOT EXISTS (
                        SELECT 1 FROM "GamePlayers" AS worse
                        WHERE worse."GameId" = me."GameId"
                            AND worse."BadPoints" - worse."Doubles" > me."BadPoints" - me."Doubles");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLoser",
                table: "GamePlayers");
        }
    }
}
