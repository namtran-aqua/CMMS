using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CMMS.Client.Common
{
    public class CurrentUser
    {
        private readonly HttpClient _http;
        private readonly AuthenticationStateProvider _authStateProvider;

        public CurrentUser(HttpClient http, AuthenticationStateProvider authStateProvider)
        {
            _http = http;
            _authStateProvider = authStateProvider;
        }

        public async Task<UserDto> LoadCurrentUser()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Guid.TryParse(userIdStr, out var userId);

                var users = new CurrentUserInfo
                {
                    UserId = userId,
                    WorkDayId = user.FindFirst("WorkDayId")?.Value ?? "",
                    FullName = user.FindFirst("FullName")?.Value ?? "",
                    Email = user.FindFirst(ClaimTypes.Email)?.Value ?? ""
                };

                var CurrenUserInfo = await _http.GetFromJsonAsync<UserDto>($"api/user/get-currentUser/{users.UserId}");
                return CurrenUserInfo ?? new UserDto();
            }

            return new UserDto();
        }
    }
}
