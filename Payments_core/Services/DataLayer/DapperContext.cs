using Dapper;
using MySqlConnector;
using System.Data;

namespace Payments_core.Services.DataLayer
{
    public class DapperContext : IDapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("SqlConnection") ?? "";
        }
        public IDbTransaction BeginTransaction()
        {
            var conn = CreateConnection();
            conn.Open();
            return conn.BeginTransaction();
        }

        public IDbConnection CreateConnection()
            => new MySqlConnection(_connectionString);

        // ======================
        // GET LIST
        // ======================
        public async Task<IEnumerable<T>> GetData<T>(string query, DynamicParameters? parameters)
        {
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<T>(
                    query,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        // ======================
        // GET SINGLE ROW
        // ======================
        public async Task<T> GetSingleData<T>(string query, DynamicParameters? parameters)
        {
            using (var connection = CreateConnection())
            {
                return await connection.QuerySingleAsync<T>(
                    query,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        // ======================
        // INSERT / UPDATE
        // ======================
        public async Task<int> SetData(string query, DynamicParameters parameters)
        {
            using (var connection = CreateConnection())
            {
                return await connection.ExecuteAsync(
                    query,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        // ======================
        // GENERIC EXECUTE WITH OBJECT PARAMS
        // ======================
        public async Task<int> ExecuteAsync(
            string sql,
            object? parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            IDbTransaction? transaction = null)
        {
            if (transaction != null)
            {
                return await transaction.Connection.ExecuteAsync(
                    sql,
                    parameters,
                    transaction: transaction,
                    commandType: commandType
                );
            }

            using (var conn = CreateConnection())
            {
                return await conn.ExecuteAsync(
                    sql,
                    parameters,
                    commandType: commandType
                );
            }
        }


        // ======================
        // GENERIC GET WITH OBJECT PARAMS
        // ======================
        public async Task<IEnumerable<T>> GetData<T>(string spName, object? parameters = null)
        {
            using (var conn = CreateConnection())
            {
                return await conn.QueryAsync<T>(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }

        // ======================
        // STORED PROC EXECUTE
        // ======================
        public async Task<int> ExecuteStoredAsync(string spName, object? parameters = null)
        {
            using (var conn = CreateConnection())
            {
                return await conn.ExecuteAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }


        public async Task<SqlMapper.GridReader> QueryMultipleAsync(
     string sql,     object? parameters = null,     CommandType commandType = CommandType.StoredProcedure,
     IDbTransaction? transaction = null
 )
        {
            if (transaction != null)
            {
                return await transaction.Connection.QueryMultipleAsync(
                    sql,
                    parameters,
                    transaction: transaction,
                    commandType: commandType
                );
            }

            var conn = CreateConnection();

            // Use synchronous Open() since IDbConnection doesn't have OpenAsync
            conn.Open();

            // Call Dapper's QueryMultipleAsync
            return await conn.QueryMultipleAsync(
                sql,
                parameters,
                commandType: commandType
            );
        }


    }
}
