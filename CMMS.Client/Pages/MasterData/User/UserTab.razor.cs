using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using AntDesign;
using CMMS.Shared.Dtos.User;
using CMMS.Shared.Dtos.Equipment;
using System.Timers;
using System.Text.Json;

namespace CMMS.Client.Pages.MasterData.User
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class UserTab : ComponentBase
    {
        [Inject] public HttpClient Http { get; set; } = default!;
        [Inject] public IMessageService Message { get; set; } = default!;
        [Inject] public Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private List<UserDto> Users = new();
        private bool isLoading = true;
        
        // RBAC Permissions
        private bool CanCreate = false;
        private bool CanEdit = false;
        private bool CanDisable = false;
        
        // Modal State
        private bool isModalVisible = false;
        private bool isEditMode = false;
        private bool isSaving = false;
        
        // Form Models
        private Guid? selectedUserId;
        private int? selectedFacId;
        private int? selectedDeptId;
        private int? selectedRoleId;
        private bool selectedIsActive = true;
        
        // Dropdown Data
        private List<AquaUserDto> AquaUsers = new();
        private List<FactoryDto> Factories = new();
        private List<DepartmentDto> Departments = new();
        private List<RoleDto> Roles = new();
        
        // Debounce timer for Search
        private System.Timers.Timer _searchTimer;
        private string _lastSearchTerm = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity?.IsAuthenticated == true)
            {
                var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "Admin" || role == "IT")
                {
                    CanCreate = true;
                    CanEdit = true;
                    CanDisable = true;
                }
            }

            _searchTimer = new System.Timers.Timer(300);
            _searchTimer.Elapsed += OnSearchTimerElapsed;
            _searchTimer.AutoReset = false;

            await LoadUsers();
            await LoadFactories();
            await LoadRoles();
        }

        private async Task LoadUsers()
        {
            isLoading = true;
            try
            {
                var response = await Http.GetFromJsonAsync<List<UserDto>>("api/user/get-all");
                if (response != null)
                {
                    Users = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading users: {ex.Message}");
                Message.Error("Failed to load users");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadFactories()
        {
            try
            {
                var response = await Http.GetFromJsonAsync<List<FactoryDto>>("api/factory/factories");
                if (response != null)
                {
                    Factories = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading factories: {ex.Message}");
            }
        }

        private async Task LoadDepartments(int factoryId)
        {
            try
            {
                var response = await Http.GetFromJsonAsync<List<DepartmentDto>>($"api/department/factory/{factoryId}");
                if (response != null)
                {
                    Departments = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading departments: {ex.Message}");
            }
        }

        private async Task LoadRoles()
        {
            try
            {
                var response = await Http.GetFromJsonAsync<List<RoleDto>>("api/user/roles");
                if (response != null)
                {
                    Roles = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading roles: {ex.Message}");
            }
        }

        private async Task OnFactoryChanged(int? newValue)
        {
            selectedFacId = newValue;
            selectedDeptId = null; // reset
            Departments.Clear();
            if (newValue.HasValue)
            {
                await LoadDepartments(newValue.Value);
            }
        }

        private void OnUserSearch(string value)
        {
            _lastSearchTerm = value ?? string.Empty;
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void OnSearchTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            await InvokeAsync(async () =>
            {
                try
                {
                    var response = await Http.GetFromJsonAsync<List<AquaUserDto>>($"api/user/aqua-users?keyword={Uri.EscapeDataString(_lastSearchTerm)}");
                    if (response != null)
                    {
                        AquaUsers = response;
                        StateHasChanged();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Search error: {ex.Message}");
                }
            });
        }

        private void ShowAddModal()
        {
            isEditMode = false;
            selectedUserId = null;
            selectedFacId = null;
            selectedDeptId = null;
            selectedRoleId = null;
            selectedIsActive = true;
            Departments.Clear();
            isModalVisible = true;
            
            // Trigger initial empty search to load top users
            OnUserSearch("");
        }

        private async Task ShowEditModal(UserDto user)
        {
            isEditMode = true;
            selectedUserId = user.Id;
            selectedFacId = user.FACID;
            selectedRoleId = user.RoleID;
            selectedIsActive = user.IsActive;
            
            // Add current user to list so dropdown displays it correctly
            AquaUsers = new List<AquaUserDto>
            {
                new AquaUserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    WorkDayId = user.WorkDayId,
                    Email = user.Email
                }
            };
            
            if (selectedFacId.HasValue)
            {
                await LoadDepartments(selectedFacId.Value);
            }
            selectedDeptId = user.DeptID;
            
            isModalVisible = true;
        }

        private async Task HandleOk()
        {
            if (selectedUserId == null)
            {
                Message.Error("Please select a User.");
                return;
            }
            if (selectedFacId == null)
            {
                Message.Error("Please select a Factory.");
                return;
            }
            if (selectedDeptId == null)
            {
                Message.Error("Please select a Department.");
                return;
            }
            if (selectedRoleId == null)
            {
                Message.Error("Please select a Role.");
                return;
            }

            isSaving = true;
            try
            {
                if (isEditMode)
                {
                    var req = new UpdateUserRequest
                    {
                        UserId = selectedUserId.Value,
                        FacId = selectedFacId.Value,
                        DeptId = selectedDeptId.Value,
                        RoleId = selectedRoleId.Value,
                        IsActive = selectedIsActive
                    };
                    var res = await Http.PutAsJsonAsync("api/user/update", req);
                    if (res.IsSuccessStatusCode)
                    {
                        Message.Success("User updated successfully");
                        isModalVisible = false;
                        await LoadUsers();
                    }
                    else
                    {
                        var error = await res.Content.ReadAsStringAsync();
                        Message.Error($"Failed to update: {error}");
                    }
                }
                else
                {
                    var req = new CreateUserRequest
                    {
                        UserId = selectedUserId.Value,
                        FacId = selectedFacId.Value,
                        DeptId = selectedDeptId.Value,
                        RoleId = selectedRoleId.Value
                    };
                    var res = await Http.PostAsJsonAsync("api/user/create", req);
                    if (res.IsSuccessStatusCode)
                    {
                        Message.Success("User added successfully");
                        isModalVisible = false;
                        await LoadUsers();
                    }
                    else
                    {
                        var error = await res.Content.ReadAsStringAsync();
                        Message.Error($"Failed to add: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Message.Error($"Error: {ex.Message}");
            }
            finally
            {
                isSaving = false;
            }
        }

        private void HandleCancel()
        {
            isModalVisible = false;
        }
        
        private async Task DisableUser(Guid id)
        {
            try
            {
                var res = await Http.PutAsync($"api/user/{id}/disable", null);
                if (res.IsSuccessStatusCode)
                {
                    Message.Success("User disabled successfully");
                    await LoadUsers();
                }
                else
                {
                    var error = await res.Content.ReadAsStringAsync();
                    Message.Error($"Failed to disable: {error}");
                }
            }
            catch (Exception ex)
            {
                Message.Error($"Error: {ex.Message}");
            }
        }
    }
}
