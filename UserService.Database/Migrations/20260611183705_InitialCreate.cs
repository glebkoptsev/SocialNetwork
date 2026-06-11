using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UserService.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app_users");

            migrationBuilder.CreateTable(
                name: "feed_outbox",
                schema: "app_users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kafka_key = table.Column<string>(type: "text", nullable: false),
                    kafka_value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feed_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "posts",
                schema: "app_users",
                columns: table => new
                {
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    post = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creation_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_posts", x => x.post_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "app_users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    second_name = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    birthdate = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    biography = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    city = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    can_publish_messages = table.Column<bool>(type: "boolean", nullable: false),
                    login = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "friends",
                schema: "app_users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    friend_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_friends", x => new { x.user_id, x.friend_id });
                    table.ForeignKey(
                        name: "fk_friends_users_friend_id",
                        column: x => x.friend_id,
                        principalSchema: "app_users",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_friends_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "app_users",
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_friends_friend_id",
                schema: "app_users",
                table: "friends",
                column: "friend_id");

            migrationBuilder.CreateIndex(
                name: "posts_userid_idx",
                schema: "app_users",
                table: "posts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "users_fname_sname_idx",
                schema: "app_users",
                table: "users",
                columns: new[] { "first_name", "second_name" })
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "users_login_idx",
                schema: "app_users",
                table: "users",
                column: "login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feed_outbox",
                schema: "app_users");

            migrationBuilder.DropTable(
                name: "friends",
                schema: "app_users");

            migrationBuilder.DropTable(
                name: "posts",
                schema: "app_users");

            migrationBuilder.DropTable(
                name: "users",
                schema: "app_users");
        }
    }
}
