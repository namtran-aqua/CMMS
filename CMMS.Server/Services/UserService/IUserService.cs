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
    }
}
