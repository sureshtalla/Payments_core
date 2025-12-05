using Dapper;
using System.Data;

namespace Payments_core.Services.DataLayer
{
    public interface IDapperContext
    {
        // Transaction
        IDbTransaction BeginTransaction();

        // EXECUTE stored procedure (no transaction)
        Task<int> ExecuteStoredAsync(string spName, object? parameters = null);

        // GET LIST (DynamicParameters)
        Task<IEnumerable<T>> GetData<T>(string query, DynamicParameters? parameters);

        // GET SINGLE ROW (DynamicParameters)
        Task<T> GetSingleData<T>(string query, DynamicParameters? parameters);

        // INSERT / UPDATE (DynamicParameters)
        Task<int> SetData(string query, DynamicParameters parameters);

        // GENERIC EXECUTE (optional transaction + optional commandType)
        Task<int> ExecuteAsync(
            string sql,
            object? parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            IDbTransaction? transaction = null
        );

        // GENERIC GET LIST (object parameters)
        Task<IEnumerable<T>> GetData<T>(string spName, object? parameters = null);

        // MULTI-QUERY (QueryMultiple)
        Task<SqlMapper.GridReader> QueryMultipleAsync(
            string sql,
            object? parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            IDbTransaction? transaction = null
        );
    }
}
