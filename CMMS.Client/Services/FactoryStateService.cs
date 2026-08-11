using CMMS.Shared.Dtos.Equipment;

namespace CMMS.Client.Services
{
    /// <summary>
    /// Singleton service lưu factory và department đang được chọn.
    /// Các page subscribe OnChange để tự refresh khi factory/department thay đổi.
    /// </summary>
    public class FactoryStateService
    {
        // null = "All Factories"
        public int? SelectedFacId { get; private set; } = null;
        public string SelectedFacName { get; private set; } = "All Factories";

        // null = "All Departments"
        public int? SelectedDeptId { get; private set; } = null;
        public string SelectedDeptName { get; private set; } = "All Departments";

        // Danh sách factory và department dùng để populate dropdown
        public List<FactoryOption> Factories { get; set; } = new();
        public List<DepartmentDto> AllDepartments { get; set; } = new();
        public List<DepartmentDto> FilteredDepartments { get; private set; } = new();

        public event Action? OnChange;

        public void SetFactory(int? facId, string facName)
        {
            SelectedFacId = facId;
            SelectedFacName = facId.HasValue ? facName : "All Factories";
            
            // Filter departments based on selected factory
            if (facId.HasValue)
            {
                FilteredDepartments = AllDepartments.Where(d => d.FACID == facId).ToList();
            }
            else
            {
                FilteredDepartments = AllDepartments;
            }

            // Reset department when factory changes
            SelectedDeptId = null;
            SelectedDeptName = "All Departments";

            OnChange?.Invoke();
        }

        public void SetDepartment(int? deptId, string deptName)
        {
            SelectedDeptId = deptId;
            SelectedDeptName = deptId.HasValue ? deptName : "All Departments";
            OnChange?.Invoke();
        }

        /// <summary>
        /// Build danh sách factory từ departments (distinct theo FACID).
        /// </summary>
        public void LoadFactoriesFromDepartments(List<DepartmentDto> departments)
        {
            AllDepartments = departments;
            
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

            // Default initialize FilteredDepartments
            if (SelectedFacId.HasValue)
            {
                FilteredDepartments = AllDepartments.Where(d => d.FACID == SelectedFacId).ToList();
            }
            else
            {
                FilteredDepartments = AllDepartments;
            }
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
