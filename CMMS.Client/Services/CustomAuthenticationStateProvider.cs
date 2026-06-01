using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ISessionStorageService _sessionStorage;
    private readonly NavigationManager _navigation;

    public CustomAuthenticationStateProvider(ISessionStorageService sessionStorage, NavigationManager navigation)
    {
        _sessionStorage = sessionStorage;
        _navigation = navigation;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _sessionStorage.GetItemAsync<string>("authToken");
        var currentUri = _navigation.Uri;

        bool isLoginPage = currentUri.EndsWith("/login", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(token) || token.Count(c => c == '.') != 2)
        {
            if (!isLoginPage)
            {
                _navigation.NavigateTo($"{_navigation.BaseUri}login", forceLoad: false);
            }
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                await _sessionStorage.RemoveItemAsync("authToken");
                if (!isLoginPage)
                {
                    _navigation.NavigateTo($"{_navigation.BaseUri}login", forceLoad: false);
                }
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }
        catch
        {
            if (!isLoginPage)
            {
                _navigation.NavigateTo($"{_navigation.BaseUri}login", forceLoad: false);
            }
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public void MarkUserAsAuthenticated(string username, List<Claim> claims)
    {
        var identity = new ClaimsIdentity(claims, "apiauth");
        var user = new ClaimsPrincipal(identity);

        var authState = new AuthenticationState(user);
        NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public void MarkUserAsLoggedOut()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }
}
