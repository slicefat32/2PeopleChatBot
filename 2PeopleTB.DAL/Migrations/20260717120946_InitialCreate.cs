using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2PeopleTB.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromChatId = table.Column<long>(type: "bigint", nullable: false),
                    ToChatId = table.Column<long>(type: "bigint", nullable: false),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    FileId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegisteredUsers",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegisteredUsers", x => x.ChatId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_FromChatId",
                table: "MessageHistories",
                column: "FromChatId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_SentAt",
                table: "MessageHistories",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistories_ToChatId",
                table: "MessageHistories",
                column: "ToChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageHistories");

            migrationBuilder.DropTable(
                name: "RegisteredUsers");
        }
    }
}
