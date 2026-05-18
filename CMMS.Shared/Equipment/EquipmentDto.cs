using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.EquipmentDto
{
    public class EquipmentDto
    {
        public int Id { get; set; }
        public string? Status { get; set; }
        public string? StatusUsing { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string? EquipmentCode { get; set; }
        public string? EquipmentBarcode { get; set; }
        public string? EquipmentModel { get; set; }
        public string? EquipmentSerial { get; set; }
        public string? EquipmentDescription { get; set; }
        public string? EquipmentNote { get; set; }
        public string? Location { get; set; }
        public int? DeptId { get; set; }
        public int? FactoryId { get; set; }
        public string? DeptName { get; set; }
        public string? FACName { get; set; }
        public DateTime? BuyDate { get; set; }
        public string? BuyPrice { get; set; }
        public string? BuyCurrency { get; set; }
        public int? MaintenanceCircleTime { get; set; }
        public bool InSAP { get; set; }
        public string? SAPCode { get; set; }
        public string? PIC { get; set; }
        public string? Vendor { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string? ContactNo { get; set; }

    } 
}
