using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace CMMS.Client.Services
{
    public class PermissionState
    {
        private readonly HttpClient _httpClient;
        private HashSet<string> _permissions = new HashSet<string>();

        public event Action? OnPermissionsChanged;

        public PermissionState(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task LoadPermissionsAsync()
        {
            try
            {
                var perms = await _httpClient.GetFromJsonAsync<List<string>>("api/Auth/my-permissions");
                if (perms != null)
                {
                    _permissions = new HashSet<string>(perms);
                }
            }
            catch
            {
                _permissions = new HashSet<string>();
            }
            finally
            {
                OnPermissionsChanged?.Invoke();
            }
        }

        public void ClearPermissions()
        {
            _permissions.Clear();
            OnPermissionsChanged?.Invoke();
        }

        public bool HasPermission(string permission)
        {
            return _permissions.Contains(permission);
        }
    }
}
