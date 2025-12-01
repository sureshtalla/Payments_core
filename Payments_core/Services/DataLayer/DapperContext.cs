using Dapper;
using MySqlConnector;
using System.Data;

namespace Payments_core.Services.DataLayer
{
    public class DapperContext:IDapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("SqlConnection") ?? "";
        }

        public IDbConnection CreateConnection()
        { return new MySqlConnection(_connectionString); }

        public async Task<IEnumerable<T>> GetData<T>(string query, DynamicParameters? parameters)
        {
            using (var connection = CreateConnection())
            {
                if (parameters != null)
                    return await connection.QueryAsync<T>(query, commandType: CommandType.StoredProcedure, param: parameters);
                else
                    return await connection.QueryAsync<T>(query, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<T> GetSingleData<T>(string query, DynamicParameters? parameters)
        {
            using (var connection = CreateConnection())
            {
                if (parameters != null)
                    return await connection.QuerySingleAsync<T>(query, commandType: CommandType.StoredProcedure, param: parameters);
                else
                    return await connection.QuerySingleAsync<T>(query, commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<int> SetData(string query, DynamicParameters parameters)
        {
            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(query, commandType: CommandType.StoredProcedure, param: parameters);
            }
        }
    }
}
