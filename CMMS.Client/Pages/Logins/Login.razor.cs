using CMMS.Shared.Dtos.AuthModels;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Logins;

public partial class Login
{
    [Inject] private HttpClient Http { get; set; }
    [Inject] private CustomAuthenticationStateProvider AuthProvider { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    [Inject] private ISessionStorageService _sessionStorage { get; set; }
    private string username { get; set; }
    private string password { get; set; }
    private bool showPassword = false;

    private async Task DoLogin()
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(username))
        {
            Message.Error("username cannot be blank");
            return;
        }
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrEmpty(password))
        {
            Message.Error("password cannot be blank");
            return;
        }


        var response = await Http.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            UserName = username,
            Password = password
        });
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Message.Error("Incorrect Account or Password, Please Check Again !");
            return;
        }
        if (!response.IsSuccessStatusCode)
        {
            Message.Error($"Server error: {(int)response.StatusCode}");
            return;
        }
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        if (content == null || !content.ContainsKey("token"))
        {
            Message.Error("Invalid response from server.");
            return;
        }
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(content["token"]);
        var claims = token.Claims.ToList();

        await _sessionStorage.SetItemAsync("authToken", content["token"]);
        AuthProvider.MarkUserAsAuthenticated(username, claims);

        var baseUri = Nav.BaseUri.TrimEnd('/');
        Nav.NavigateTo($"{baseUri}/");

    }
    private void TogglePassword()
    {
        showPassword = !showPassword;
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await DoLogin();
        }
    }
}
