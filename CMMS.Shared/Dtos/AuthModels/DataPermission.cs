namespace CMMS.Shared.Dtos.AuthModels
{
    public class DataPermission
    {
        public bool IsGlobal { get; set; }
        public int? FacId { get; set; }
        public int? DeptId { get; set; }
    }
}
