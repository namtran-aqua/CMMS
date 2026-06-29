using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Client.Services
{
    /// <summary>
    /// Singleton service lưu factory đang được chọn (null = All Factories).
    /// Các page subscribe OnChange để tự refresh khi factory thay đổi.
    /// </summary>
    public class FactoryStateService
    {
        // null = "All Factories"
        public int? SelectedFacId { get; private set; } = null;
        public string SelectedFacName { get; private set; } = "All Factories";

        // Danh sách factory dùng để populate dropdown
        public List<FactoryOption> Factories { get; set; } = new();

        public event Action? OnChange;

        public void SetFactory(int? facId, string facName)
        {
            SelectedFacId = facId;
            SelectedFacName = facId.HasValue ? facName : "All Factories";
            OnChange?.Invoke();
        }

        /// <summary>
        /// Build danh sách factory từ departments (distinct theo FACID).
        /// </summary>
        public void LoadFactoriesFromDepartments(List<DepartmentDto> departments)
        {
            Factories = departments
                .Where(d => d.FACID.HasValue && !string.IsNullOrEmpty(d.FACName))
                .GroupBy(d => d.FACID)
                .Select(g => new FactoryOption
                {
                    FacId   = g.Key!.Value,
                    FacName = g.First().FACName ?? "",
                    FacCode = g.First().FACCode ?? ""
                })
                .OrderBy(f => f.FacCode)
                .ToList();
        }
    }

    public class FactoryOption
    {
        public int FacId { get; set; }
        public string FacName { get; set; } = "";
        public string FacCode { get; set; } = "";
        public string DisplayName => $"{FacCode} - {FacName}";
    }
}
