using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserService.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS idx_users_search_trgm " +
                "ON app_users.users USING gin ((first_name || ' ' || second_name || ' ' || login) gin_trgm_ops)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_users_search_trgm");
        }
    }
}
