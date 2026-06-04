using Npgsql;

namespace Libraries.NpgsqlService
{
    public interface INpgsqlService
    {
        Task<int> ExecuteNonQueryAsync(string query, NpgsqlParameter[] parameters);
        Task<List<Dictionary<string, object>>> GetQueryResultAsync(string query, NpgsqlParameter[] parameters, string[] columns, TargetSessionAttributes targetSessionAttributes = TargetSessionAttributes.Any);
    }
}
