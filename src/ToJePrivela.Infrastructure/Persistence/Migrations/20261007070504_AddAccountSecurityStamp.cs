using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Accounts",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            // Every existing login gets its own random stamp (32 hex characters, like Account.NewSecurityStamp). Tokens
            // issued before carry no stamp, so their holders sign in once more.
            migrationBuilder.Sql("UPDATE Accounts SET SecurityStamp = lower(hex(randomblob(16)));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Accounts");
        }
    }
}
