using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastSignedInAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Provider_Email",
                table: "Accounts",
                columns: new[] { "Provider", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Provider_ExternalId",
                table: "Accounts",
                columns: new[] { "Provider", "ExternalId" },
                unique: true);

            // Account.ReservedId: owns what was stored before sign-in until the first seeded admin takes it over. It is
            // inserted here rather than seeded through the model, so a later model change can never reset the admin
            // who took it. Its provider means nothing until then.
            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "Provider", "ExternalId", "Email", "DisplayName", "IsAdmin", "LastSignedInAt" },
                values: new object[] { 1, "Google", null, null, "Reserved", false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
