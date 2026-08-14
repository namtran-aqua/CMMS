using AntDesign;
using CMMS.Shared.Dtos.AuthModels;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CMMS.Client.Pages.MasterData.RolePermission
{
    public partial class RolePermissionTab
    {
        [Inject] public HttpClient Http { get; set; } = default!;
        [Inject] public IMessageService Message { get; set; } = default!;

        private bool loadingRoles = false;
        private bool loadingPermissions = false;
        private bool saving = false;

        private List<RoleDto> roles = new();
        private List<PermissionPageDto> permissionPages = new();
        private RoleDto? selectedRole;
        private string[] selectedRoleKeys = new string[] { };

        protected override async Task OnInitializedAsync()
        {
            await LoadRolesAsync();
        }

        private async Task LoadRolesAsync()
        {
            loadingRoles = true;
            try
            {
                var response = await Http.GetFromJsonAsync<List<RoleDto>>("api/rolepermission/roles");
                if (response != null)
                {
                    roles = response;
                }
            }
            catch (System.Exception ex)
            {
                Message.Error($"Error loading roles: {ex.Message}");
            }
            finally
            {
                loadingRoles = false;
            }
        }

        private async Task OnRoleSelected(MenuItem item)
        {
            var roleIdStr = item.Key;
            if (int.TryParse(roleIdStr, out var roleId))
            {
                selectedRole = roles.FirstOrDefault(r => r.RoleID == roleId);
                selectedRoleKeys = new[] { roleIdStr };
                await LoadPermissionsAsync(roleId);
            }
        }

        private async Task LoadPermissionsAsync(int roleId)
        {
            loadingPermissions = true;
            try
            {
                var response = await Http.GetFromJsonAsync<List<PermissionPageDto>>($"api/rolepermission/permissions/{roleId}");
                if (response != null)
                {
                    permissionPages = response;
                }
            }
            catch (System.Exception ex)
            {
                Message.Error($"Error loading permissions: {ex.Message}");
            }
            finally
            {
                loadingPermissions = false;
            }
        }

        private async Task SavePermissions()
        {
            if (selectedRole == null) return;

            saving = true;
            try
            {
                var grantedIds = new List<int>();
                foreach (var page in permissionPages)
                {
                    grantedIds.AddRange(page.Permissions.Where(p => p.IsGranted).Select(p => p.PermissionID));
                }

                var request = new UpdateRolePermissionsRequest
                {
                    RoleID = selectedRole.RoleID,
                    PermissionIDs = grantedIds
                };

                var response = await Http.PostAsJsonAsync("api/rolepermission/update", request);
                if (response.IsSuccessStatusCode)
                {
                    Message.Success("Permissions saved successfully!");
                }
                else
                {
                    Message.Error("Failed to save permissions.");
                }
            }
            catch (System.Exception ex)
            {
                Message.Error($"Error saving permissions: {ex.Message}");
            }
            finally
            {
                saving = false;
            }
        }

        private string GetModuleName(string code)
        {
            return code switch
            {
                "SPAREPART" => "Spare Part",
                "EQUIPMENT" => "Equipment",
                "MAINTENANCE" => "Maintenance",
                "MASTERDATA" => "Master Data",
                _ => code
            };
        }
    }
}
