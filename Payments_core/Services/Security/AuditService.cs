using Payments_core.Services.DataLayer;

namespace Payments_core.Services.Security
{
    public class AuditService
    {
        private readonly IDapperContext _db;

        public AuditService(IDapperContext db)
        {
            _db = db;
        }

        public async Task InsertAuditLog(
            long? userId,
            string action,
            string entityType,
            long? entityId,
            string details,
            string ipAddress)
        {
            await _db.ExecuteStoredAsync(
                "sp_insert_audit_log",
                new
                {
                    p_user_id = userId,
                    p_action = action,
                    p_entity_type = entityType,
                    p_entity_id = entityId,
                    p_details = details,
                    p_ip_address = ipAddress
                });
        }
    }
}