using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Libraries.NpgsqlService
{
    public class NpgsqlService : IAsyncDisposable, IDisposable, INpgsqlService
    {
        private NpgsqlMultiHostDataSource Npgsql { get; }
        public NpgsqlService(IConfiguration configuration)
        {
            var connectionStringName = "postgres";
#if DEBUG
            connectionStringName = "postgres_debug";
#endif
            var connectionString = configuration.GetConnectionString(connectionStringName)
                ?? throw new Exception("connection string not found");
            Npgsql = new NpgsqlDataSourceBuilder(connectionString).BuildMultiHost();
        }

        public void Dispose()
        {
            Npgsql.Dispose();
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await Npgsql.DisposeAsync();
            GC.SuppressFinalize(this);
        }

        public async Task ExecuteTransactionAsync(string[] queries, NpgsqlParameter[][] parameters)
        {
            await using var connection = await Npgsql.OpenConnectionAsync(TargetSessionAttributes.Primary);
            await using var transaction = await connection.BeginTransactionAsync();
            await using var cmd = new NpgsqlCommand(queries[0], connection, transaction);

            for (int i = 0; i < queries.Length; i++)
            {
                cmd.CommandText = queries[i];
                cmd.Parameters.Clear();
                if (parameters[i].Length > 0)
                    cmd.Parameters.AddRange(parameters[i]);
                await cmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        public async Task<int> ExecuteNonQueryAsync(string query, NpgsqlParameter[] parameters)
        {
            await using var connection = await Npgsql.OpenConnectionAsync(TargetSessionAttributes.Primary);
            await using var cmd = new NpgsqlCommand(query, connection);
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Dictionary<string, object>>> GetQueryResultAsync(string query, NpgsqlParameter[] parameters, string[] columns, TargetSessionAttributes targetSessionAttributes = TargetSessionAttributes.Any)
        {
            List<Dictionary<string, object>> result = [];
            await using var connection = await Npgsql.OpenConnectionAsync(targetSessionAttributes);
            await using var cmd = new NpgsqlCommand(query, connection);
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                foreach (var column in columns)
                {
                    row.Add(column, reader.GetValue(column));
                }
                result.Add(row);
            }
            return result;
        }
    }
}
