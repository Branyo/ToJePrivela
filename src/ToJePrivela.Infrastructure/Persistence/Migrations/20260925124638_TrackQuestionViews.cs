using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ToJePrivela.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TrackQuestionViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewedAt",
                table: "Questions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Questions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_CategoryId_ViewCount",
                table: "Questions",
                columns: new[] { "CategoryId", "ViewCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Questions_CategoryId_ViewCount",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "LastViewedAt",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Questions");
        }
    }
}
