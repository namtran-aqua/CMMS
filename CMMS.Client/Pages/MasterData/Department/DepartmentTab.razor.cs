using AntDesign;
using CMMS.Shared.Dtos.Equipment;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.MasterData.Department
{
    public partial class DepartmentTab
    {
        [Inject]
        public HttpClient Http { get; set; }

        [Inject]
        public IMessageService Message { get; set; }

        public List<DepartmentDto> Departments { get; set; } = new();
        public DepartmentDto editingDepartment { get; set; } = new();
        public bool isDepartmentModalVisible = false;
        public string modalTitle = "Add Department";
        public bool isEditMode = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadDepartments();
        }

        public async Task LoadDepartments()
        {
            try
            {
                Departments = await Http.GetFromJsonAsync<List<DepartmentDto>>("api/Department/get-all") ?? new List<DepartmentDto>();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Message.Error("Failed to load departments: " + ex.Message);
            }
        }

        public void ShowAddDepartmentModal()
        {
            editingDepartment = new DepartmentDto();
            modalTitle = "Add Department";
            isEditMode = false;
            isDepartmentModalVisible = true;
        }

        public void ShowEditDepartmentModal(DepartmentDto department)
        {
            editingDepartment = new DepartmentDto
            {
                DeptID = department.DeptID,
                FACID = department.FACID,
                DeptCode = department.DeptCode,
                DeptName = department.DeptName,
                HODWD = department.HODWD
            };
            modalTitle = "Edit Department";
            isEditMode = true;
            isDepartmentModalVisible = true;
        }

        public async Task HandleDepartmentOk()
        {
            if (string.IsNullOrWhiteSpace(editingDepartment.DeptName))
            {
                Message.Warning("Department Name is required.");
                return;
            }

            HttpResponseMessage response;
            if (isEditMode)
            {
                response = await Http.PutAsJsonAsync("api/Department", editingDepartment);
            }
            else
            {
                response = await Http.PostAsJsonAsync("api/Department", editingDepartment);
            }

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    isDepartmentModalVisible = false;
                    await LoadDepartments();
                }
                else
                {
                    Message.Error(result?.Message ?? "Action failed.");
                }
            }
            else
            {
                Message.Error("Network error occurred.");
            }
        }

        public void HandleDepartmentCancel()
        {
            isDepartmentModalVisible = false;
        }

        public async Task DeleteDepartment(int? id)
        {
            if (id == null) return;
            var response = await Http.DeleteAsync($"api/Department/{id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CMMS.Shared.Dtos.Common.ApiResponse>();
                if (result != null && result.Success)
                {
                    Message.Success(result.Message);
                    await LoadDepartments();
                }
                else
                {
                    Message.Error(result?.Message ?? "Delete failed.");
                }
            }
            else
            {
                Message.Error("Network error occurred.");
            }
        }
    }
}
