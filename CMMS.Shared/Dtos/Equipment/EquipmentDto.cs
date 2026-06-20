using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Equipment
{
    public class EquipmentDto
    {
        public int EQID { get; set; }
        public int? ETypeID { get; set; }
        public string? ETypeName { get; set; }
        public string? ETypeCode { get; set; }

        public int? ECATID { get; set; }
        public string? ECATName { get; set; }
        public string? ECATCode { get; set; }

        public int? DeptID { get; set; }
        public string? DeptCode { get; set; }
        public string? DeptName { get; set; }
        public int? FACID { get; set; }
        public string? FACCode { get; set; }
        public string? FACName { get; set; }
        public int? StsUseID { get; set; }
        public string? StsUseName { get; set; }

        public string? EquipmentName { get; set; }
        public string? EquipmentCode { get; set; }
        public string? EquipmentBarcode { get; set; }
        public string? EquipmentModel { get; set; }
        public string? EquipmentSerial { get; set; }
        public string? EquipmentDescription { get; set; }
        public string? EquipmentNote { get; set; }

        public DateTime? BuyDate { get; set; }
        public int? MaintenanceCircleTime { get; set; }
        public decimal? BuyPrice { get; set; }

        public string? BuyCurrency { get; set; }

        public int? VendorID { get; set; }
        public string? ContactNo { get; set; }

        public bool? InSAP { get; set; }

        public int? SAPCode { get; set; }
        public string? LocName { get; set; }
        public string? LocCode { get; set; }
        public string? LocDescription { get; set; }
        public string? PICID { get; set; }
        public string? PIC { get; set; }

        public bool IsActive { get; set; } = true;

        public int? LocID { get; set; }
        public string? LocFACID { get; set; }

        public string? LocDeptID { get; set; }

        public int? StsMainID { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
    }
}
