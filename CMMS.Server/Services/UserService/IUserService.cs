using CMMS.Shared.Dtos.AuthModels;
using CMMS.Shared.Dtos.User;

namespace CMMS.Server.Services.UserService
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<LoginResponse?> LoginAsync(LoginRequest loginRequest);
        Task<bool> ChangePasswordAsync(ChangePassRequest request);
        Task<bool> ResetPasswordAsync(ResetPassword request);
        Task<UserDto?> GetCurrentUserAsync(Guid userId);
        Task<bool> SendOtpAsync(ForgotPasswordRequest request);
        Task<string> VerifyOtpAsync(VerifyOtpRequest request);
        Task<bool> ResetPasswordWithTokenAsync(ResetPasswordRequest request);

        // CMMS specific user management
        Task<List<AquaUserDto>> GetAquaUsersAsync(string keyword);
        Task<bool> CreateUserAsync(CreateUserRequest request, UserDto currentUser);
        Task<bool> UpdateUserAsync(UpdateUserRequest request, UserDto currentUser);
        Task<bool> DisableUserAsync(Guid userId, UserDto currentUser);
    }
}
