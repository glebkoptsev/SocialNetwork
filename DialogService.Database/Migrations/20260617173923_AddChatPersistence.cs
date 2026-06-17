using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DialogService.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddChatPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_messages",
                schema: "app_dialogs",
                table: "messages");

            migrationBuilder.AddColumn<int>(
                name: "status",
                schema: "app_dialogs",
                table: "messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                schema: "app_dialogs",
                table: "messages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "pk_messages",
                schema: "app_dialogs",
                table: "messages",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "messages_chatid_idx",
                schema: "app_dialogs",
                table: "messages",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "chat_users_userid_idx",
                schema: "app_dialogs",
                table: "chat_users",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_messages",
                schema: "app_dialogs",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "messages_chatid_idx",
                schema: "app_dialogs",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "chat_users_userid_idx",
                schema: "app_dialogs",
                table: "chat_users");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "app_dialogs",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "user_name",
                schema: "app_dialogs",
                table: "messages");

            migrationBuilder.AddPrimaryKey(
                name: "pk_messages",
                schema: "app_dialogs",
                table: "messages",
                columns: new[] { "message_id", "chat_id" });
        }
    }
}
