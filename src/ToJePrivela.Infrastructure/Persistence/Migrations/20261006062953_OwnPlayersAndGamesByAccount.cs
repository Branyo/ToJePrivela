using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OwnPlayersAndGamesByAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_NameKey",
                table: "Players");

            // Everything stored before logins existed belongs to the reserved account (Account.ReservedId), which the
            // first seeded admin takes over.
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Players",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.UpdateData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 1,
                column: "AccountId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 2,
                column: "AccountId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Players",
                keyColumn: "Id",
                keyValue: 3,
                column: "AccountId",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Players_AccountId_NameKey",
                table: "Players",
                columns: new[] { "AccountId", "NameKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_AccountId",
                table: "Games",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Accounts_AccountId",
                table: "Games",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Accounts_AccountId",
                table: "Players",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Accounts_AccountId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Accounts_AccountId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_AccountId_NameKey",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Games_AccountId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Games");

            migrationBuilder.CreateIndex(
                name: "IX_Players_NameKey",
                table: "Players",
                column: "NameKey",
                unique: true);
        }
    }
}
