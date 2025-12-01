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
    }
}
