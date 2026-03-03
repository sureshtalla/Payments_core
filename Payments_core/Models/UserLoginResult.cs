namespace Payments_core.Models
{
    public class UserLoginResult
    {
        public long Id { get; set; }

        public string full_name { get; set; } = string.Empty;

        public string mobile { get; set; } = string.Empty;   

        public string Email { get; set; } = string.Empty;

        public int role_id { get; set; }

        public string role_name { get; set; } = string.Empty;

        public string password_hash { get; set; } = string.Empty;

        public int failed_attempts { get; set; }

        public bool is_blocked { get; set; }

        public DateTime? blocked_until { get; set; }
    }
}