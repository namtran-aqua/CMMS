
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.SessionStorage;
using CMMS.Client.Common;
using CMMS.Shared.Dtos.User;

namespace CMMS.Client.Shared
{
    public partial class MainLayout
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] HttpClient Http { get; set; }
        [Inject] ISessionStorageService SessionStorage { get; set; }

        private UserDto? CurrentUser { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadCurrentUser();
        }

        private async Task LoadCurrentUser()
        {
            try
            {
                var currentUserClass = new CurrentUser(Http, AuthStateProvider);
                CurrentUser = await currentUserClass.LoadCurrentUser();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading current user in layout: {ex.Message}");
            }
        }

        private string GetLink(string url)
        {
            var basePath = NavigationManager.BaseUri.TrimEnd('/');
            return $"{basePath}{url}";
        }

        private async Task Logout()
        {
            await SessionStorage.RemoveItemAsync("authToken");
            if (AuthStateProvider is CustomAuthenticationStateProvider authProvider)
            {
                authProvider.MarkUserAsLoggedOut();
            }
            NavigationManager.NavigateTo($"{NavigationManager.BaseUri}login", forceLoad: true);
        }

        private void GoToLogin()
        {
            NavigationManager.NavigateTo($"{NavigationManager.BaseUri}login");
        }
    }
}

