using Payments_core.Models;
using System.Threading.Tasks;

namespace Payments_core.Services.UserDataService
{
    public interface IUserDataService
    {
        Task<long> RegisterUserAsync(UserRegisterRequest request, string passwordHash);
        Task<UserProfileResponse?> GetUserByMobileAsync(string UserName); // nullable
        Task<UserProfileResponse?> GetProfileAsync(long id);             // nullable
        Task<bool> UpdateProfileAsync(UserUpdateProfileRequest request);

        bool VerifyPassword(string plain, string hash);

        Task<bool> UpdateLoginAttemptAsync(UserProfileResponse user);
        Task<IEnumerable<UserManagementResponse?>> GetUserManagementProfile();
        Task<bool> ManageUserStatusAsync(ManageUserStatusRequest request);

        Task<string?> GetUserPasswordHashAsync(long userId);
        Task<bool> UpdateUserPasswordAsync(long userId, string passwordHash);

        Task<string?> GetUserTinNoAsync(long userId);
        Task<bool> UpdateUserTinNoAsync(long userId, string tinNo);
    }
}
