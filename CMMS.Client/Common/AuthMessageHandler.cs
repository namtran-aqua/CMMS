using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
namespace AquaSolution.Client.Common;
public class AuthMessageHandler : DelegatingHandler
{
    private readonly ISessionStorageService _sessionStorage;
    private readonly NavigationManager _navigation;

    public AuthMessageHandler(ISessionStorageService sessionStorage, NavigationManager navigation)
    {
        _sessionStorage = sessionStorage;
        _navigation = navigation;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _sessionStorage.GetItemAsync<string>("authToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Nếu bị 401 thì chuyển hướng logout
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _sessionStorage.RemoveItemAsync("authToken");
            _navigation.NavigateTo("/logout", forceLoad: true); // Redirect để xóa token và chuyển về login
        }

        return response;
    }
}
