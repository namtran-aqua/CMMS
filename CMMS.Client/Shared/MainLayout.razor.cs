

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.SessionStorage;
using CMMS.Client.Common;
using CMMS.Client.Services;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.User;
using System.Net.Http.Json;


namespace CMMS.Client.Shared
{
    public partial class MainLayout
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] HttpClient Http { get; set; }
        [Inject] ISessionStorageService SessionStorage { get; set; }
        [Inject] FactoryStateService FactoryState { get; set; }

        private UserDto? CurrentUser { get; set; }
        private List<DepartmentDto> _allDepartments = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCurrentUser();
            await LoadFactories();

            if (CurrentUser != null && CurrentUser.FACID.HasValue && !FactoryState.SelectedFacId.HasValue)
            {
                var fac = FactoryState.Factories.FirstOrDefault(f => f.FacId == CurrentUser.FACID.Value);
                FactoryState.SetFactory(CurrentUser.FACID.Value, fac?.FacName ?? "");
            }
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

        private async Task LoadFactories()
        {
            try
            {
                _allDepartments = await Http.GetFromJsonAsync<List<DepartmentDto>>("api/department/departments") ?? new();
                FactoryState.LoadFactoriesFromDepartments(_allDepartments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading factories: {ex.Message}");
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

        private void OnFactoryChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var facId))
            {
                var fac = FactoryState.Factories
                    .FirstOrDefault(f => f.FacId == facId);

                FactoryState.SetFactory(
                    facId,
                    fac?.FacName ?? "");
            }
            else
            {
                FactoryState.SetFactory(
                    null,
                    "All Factories");
            }
        }
    }
}

