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
        public required string FullName { get; set; }
        public required string Mobile { get; set; }
        public required string Email { get; set; }
        public int RoleId { get; set; }
        public long? ParentUserId { get; set; }
        public required string Status { get; set; }
        public required string BusinessName { get; set; }
        public required string TinNo { get; set; }
        public required string Token { get; set; } // only for login
        public required string password_hash { get; set; } // only for login
        
    }

    public class UserUpdateProfileRequest
    {
        public long Id { get; set; }
        public required string FullName { get; set; }
        public required string BusinessName { get; set; }
        public required string TinNo { get; set; }
    }


}
