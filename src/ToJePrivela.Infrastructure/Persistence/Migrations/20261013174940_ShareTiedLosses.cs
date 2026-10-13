using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShareTiedLosses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // StoreGameLosers backfilled finished games under the old rule, which broke a tie by the number of bad
            // cards. Under the game's rule a tie is a shared loss: everyone on the most final bad points of a finished,
            // not cancelled game with a bad card lost it. Games finished since already follow it, so this changes only
            // the backfilled ties.
            migrationBuilder.Sql(
                """
                UPDATE "GamePlayers" AS me SET "IsLoser" = NOT EXISTS (
                        SELECT 1 FROM "GamePlayers" AS worse
                        WHERE worse."GameId" = me."GameId"
                            AND worse."BadPoints" - worse."Doubles" > me."BadPoints" - me."Doubles")
                WHERE EXISTS (
                        SELECT 1 FROM "Games" AS g
                        WHERE g."Id" = me."GameId" AND g."Finished" IS NOT NULL AND g."IsCancelled" = 0)
                    AND EXISTS (
                        SELECT 1 FROM "GamePlayers" AS card WHERE card."GameId" = me."GameId" AND card."BadCards" > 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The verdicts stay: the old tie-break cannot be told apart from games finished under the current rule.
        }
    }
}
