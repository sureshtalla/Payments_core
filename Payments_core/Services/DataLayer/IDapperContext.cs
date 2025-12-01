using Dapper;

namespace Payments_core.Services.DataLayer
{
    public interface IDapperContext
    {

        Task<IEnumerable<T>> GetData<T>(string query, DynamicParameters? parameters);
        Task<T> GetSingleData<T>(string query, DynamicParameters? parameters);
        Task<int> SetData(string query, DynamicParameters parameters);


    }
}
