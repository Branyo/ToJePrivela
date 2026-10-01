using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KeepGamesOfDeletedPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GamePlayers_Players_PlayerId",
                table: "GamePlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionCategories_Players_AddedByPlayerId",
                table: "QuestionCategories");

            migrationBuilder.DropIndex(
                name: "IX_QuestionCategories_AddedByPlayerId",
                table: "QuestionCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GamePlayers",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "AddedByPlayerId",
                table: "QuestionCategories");

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Games",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "PlayerId",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GamePlayers",
                table: "GamePlayers",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayers_GameId_PlayerId",
                table: "GamePlayers",
                columns: new[] { "GameId", "PlayerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GamePlayers_Players_PlayerId",
                table: "GamePlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GamePlayers_Players_PlayerId",
                table: "GamePlayers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GamePlayers",
                table: "GamePlayers");

            migrationBuilder.DropIndex(
                name: "IX_GamePlayers_GameId_PlayerId",
                table: "GamePlayers");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "GamePlayers");

            migrationBuilder.AddColumn<int>(
                name: "AddedByPlayerId",
                table: "QuestionCategories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlayerId",
                table: "GamePlayers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_GamePlayers",
                table: "GamePlayers",
                columns: new[] { "GameId", "PlayerId" });

            migrationBuilder.UpdateData(
                table: "QuestionCategories",
                keyColumn: "Id",
                keyValue: 1,
                column: "AddedByPlayerId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "QuestionCategories",
                keyColumn: "Id",
                keyValue: 2,
                column: "AddedByPlayerId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "QuestionCategories",
                keyColumn: "Id",
                keyValue: 3,
                column: "AddedByPlayerId",
                value: 1);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionCategories_AddedByPlayerId",
                table: "QuestionCategories",
                column: "AddedByPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_GamePlayers_Players_PlayerId",
                table: "GamePlayers",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionCategories_Players_AddedByPlayerId",
                table: "QuestionCategories",
                column: "AddedByPlayerId",
                principalTable: "Players",
                principalColumn: "Id");
        }
    }
}
