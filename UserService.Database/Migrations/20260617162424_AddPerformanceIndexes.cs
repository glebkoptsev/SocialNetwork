using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "posts_userid_created_idx",
                schema: "app_users",
                table: "posts",
                columns: new[] { "user_id", "creation_datetime" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "feed_outbox_processed_idx",
                schema: "app_users",
                table: "feed_outbox",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "posts_userid_created_idx",
                schema: "app_users",
                table: "posts");

            migrationBuilder.DropIndex(
                name: "feed_outbox_processed_idx",
                schema: "app_users",
                table: "feed_outbox");
        }
    }
}
