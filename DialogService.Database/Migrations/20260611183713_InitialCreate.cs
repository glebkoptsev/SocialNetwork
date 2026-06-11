using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DialogService.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app_dialogs");

            migrationBuilder.CreateTable(
                name: "chat_users",
                schema: "app_dialogs",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creation_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_users", x => new { x.chat_id, x.user_id });
                });

            migrationBuilder.CreateTable(
                name: "chats",
                schema: "app_dialogs",
                columns: table => new
                {
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    creation_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_update_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chats", x => x.chat_id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "app_dialogs",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creation_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => new { x.message_id, x.chat_id });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_users",
                schema: "app_dialogs");

            migrationBuilder.DropTable(
                name: "chats",
                schema: "app_dialogs");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "app_dialogs");
        }
    }
}
