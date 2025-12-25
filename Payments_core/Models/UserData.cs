namespace Payments_core.Models
{
    public class UserRegisterRequest
    {
        public required string FullName { get; set; }
        public required string Mobile { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public int RoleId { get; set; }
        public long? ParentUserId { get; set; }
        public required string BusinessName { get; set; }
        public required string TinNo { get; set; }
    }
    public class UserLoginRequest
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
    public class VerifyOtpRequest
    {
        public long UserId { get; set; }
        public string Otp { get; set; }
    }
    public class UserProfileResponse
    {
        public long Id { get; set; }
        public required string full_name { get; set; }
        public required string Mobile { get; set; }
        public required string Email { get; set; }
        public int role_id { get; set; }
        public required string role_name { get; set; }
        public long? parent_user_id { get; set; }
        public required string business_name { get; set; }
        public required string Status { get; set; }

        public required string tinno { get; set; }
        public required string Token { get; set; } // only for login
        public required string password_hash { get; set; } // only for login

        public int failed_attempts { get; set; }
        public bool is_blocked { get; set; }
        public DateTime? blocked_until { get; set; }

    }

    public class UserUpdateProfileRequest
    {
        public long Id { get; set; }
        public required string FullName { get; set; }
        public required string BusinessName { get; set; }
        public required string TinNo { get; set; }
    }
    public class UserManagementResponse
    {
        public long Id { get; set; }
        public  string full_name { get; set; }
        public string MerchantName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime last_login { get; set; }
    }
    public class ManageUserStatusRequest
    {
        public long UserId { get; set; }
        public string Action { get; set; } // "status" | "unblock"
        public string StatusValue { get; set; } // REQUIRED when action = "status"

    }

}
