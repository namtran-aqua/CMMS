using AntDesign;
using CMMS.Client.Common;
using CMMS.Shared.Dtos.Equipment;
using CMMS.Shared.Dtos.Maintenance;
using CMMS.Shared.Dtos.Maintenance.Attachments;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Net.Mail;

namespace CMMS.Client.Modals.Maintenances
{
    public partial class MaintenanceModel
    {
        #region Declaration
        [Inject] private HttpClient Http { get; set; }
        private bool IsModalVisible = false;
        private MaintenanceDto MaintenanceDto { get; set; } = new();
        private List<VendorDto> Vendors = new();
        private List<UserDto> Users = new();
        private UserDto CurrentUser { get; set; } = new();
        private int eqId { get; set; }
        [Parameter] public EventCallback OnSave { get; set; }
        private Form<MaintenanceDto> formRef = new();
        private List<AttachmentDto> Attachment = new();

        private List<StatusItem> MaintenanceStatuses = new()
        {
            new() { Id = 1, Name = "Routine" },
            new() { Id = 2, Name = "Repair" }
        };

        public class StatusItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        #endregion
        #region Innit

        //private UserDto _selectedUserId;
        //private UserDto? User;

        //private UserDto SelectedUserId
        //{
        //    get => _selectedUserId;
        //    set
        //    {
        //        if (_selectedUserId != value)
        //        {
        //            _selectedUserId = value;
        //            MaintenanceDto.PICID = value?.WorkDayId;
        //            MaintenanceDto.MaintPIC = value?.FullName;
        //        }
        //    }
        //}

        public async Task ShowModal(int eqid)
        {
            var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
            CurrentUser = await CurrentUserClass.LoadCurrentUser();
            eqId = eqid;
            MaintenanceDto = new();
            await LoadVendorData();
            await LoadUsersData();
            IsModalVisible = true;
            await InvokeAsync(StateHasChanged);
        }
        #endregion
        #region Action
        private async Task Close()
        {
            IsModalVisible = false;
        }
        private async Task SaveAsync()
        {
            var selectedUser = Users.FirstOrDefault(u => u.WorkDayId == MaintenanceDto.PICID);
            MaintenanceDto.MaintPIC = selectedUser?.FullName;
            Console.WriteLine(selectedUser == null
                ? "USER NOT FOUND"
                : $"FOUND USER: {selectedUser.FullName}");
            var valid = formRef.Validate();
            if (!valid)
            {
                return;
            }
            else
            {
                await CreatedAsync(eqId);
            }    
            IsModalVisible = false;
        }
        private async Task CreatedAsync(int eqId)
        {
            try
            {
                var CurrentUserClass = new CurrentUser(Http, AuthStateProvider);
                CurrentUser = await CurrentUserClass.LoadCurrentUser();
                MaintenanceDto.WorkDayId = CurrentUser.WorkDayId;
                MaintenanceDto.Attachments = Attachment;
                var response = await Http.PostAsJsonAsync($"api/Maintenance/create/{eqId}",MaintenanceDto);
                if (response.IsSuccessStatusCode)
                {
                    Message.Success("Tạo thành công!");
                    Attachment.Clear();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Message.Error($"Tạo thất bại: {error}");
                }
                var update = await Http.PostAsync("api/equipment/update-status", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating maintenance: {ex.Message}");
            }
        }
        #endregion
        private async Task LoadVendorData()
        {
            if (Vendors != null && Vendors.Any()) return;
            Vendors = await Http.GetFromJsonAsync<List<VendorDto>>("api/vendor/vendors") ?? new();
        }
        private async Task LoadUsersData()
        {
            if (Users != null && Users.Any()) return;
            Users = await Http.GetFromJsonAsync<List<UserDto>>("api/user/users") ?? new();
        }
        #region Handle File
        private async Task Deleted(AttachmentDto file)
        {
            Attachment.Remove(file);
            var url = $"{file.FilePath}";
            var response = await Http.DeleteAsync($"api/Common/delete-file-suport?avatarUrl={url}");
            await InvokeAsync(StateHasChanged);

        }
        private async Task OnSingleCompleted(UploadInfo fileinfo)
        {
            if (fileinfo.File.State == UploadState.Success)
            {
                var url = fileinfo.File.Response?.ToString();
                if (!string.IsNullOrEmpty(url))
                {
                    var uri = new Uri(url);
                    var relativePath = uri.AbsolutePath.TrimStart('/');
                    Attachment.Add(new AttachmentDto
                    {
                        FilePath = relativePath,
                        FileName = fileinfo.File.FileName,
                        FileExtend = fileinfo.File.Ext,
                        FileSize = fileinfo.File.Size,
                        CreatedTime = DateTime.Now
                    });

                }

            }
        }
        private string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{Math.Round(bytes / (1024.0 * 1024.0))} MB";
            return $"{Math.Round(bytes / 1024.0)} KB";
        }
        private bool BeforeUpload1(UploadFileItem file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!(ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                  ext == ".docx" || ext == ".xlsx" || ext == ".pdf" ||
                  ext == ".txt" || ext == ".slx"))
            {
                Message.Error("You can only upload JPG, PNG, DOCX, XLSX, PDF, TXT or SLX files!");
                return false;
            }
            var isLt2M = file.Size / 1024 / 1024 < 5;
            if (!isLt2M)
            {
                Message.Error("File must be smaller than 3MB!");
                return false;
            }

            return true;
        }
        #endregion
    }
}
