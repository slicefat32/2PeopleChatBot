using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2PeopleTB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedTelegramUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedTelegramUpdates",
                columns: table => new
                {
                    UpdateId = table.Column<int>(type: "int", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedTelegramUpdates", x => x.UpdateId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedTelegramUpdates");
        }
    }
}
