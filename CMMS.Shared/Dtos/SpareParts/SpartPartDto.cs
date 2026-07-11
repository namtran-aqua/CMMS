using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartDto
    {
        public int SPID { get; set; }
        public string? PartName { get; set; }
        public string? PartCode { get; set; }
        public string? Unit { get; set; }
        public decimal? Price { get; set; }
        public int? Inventory { get; set; }
        public int? MinStock { get; set; }
        public int? LocID { get; set; }
        public int? CategoryID { get; set; }
        public int? SupplierID { get; set; }
        public string? Vendor { get => SupplierName; set => SupplierName = value; }
        public string? Note { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public Guid? CreateBy { get; set; }
        public Guid? UpdateBy { get; set; }

        // Aliased properties to map database columns and frontend bindings
        public string? Code { get => PartCode; set => PartCode = value; }
        public string? Name { get => PartName; set => PartName = value; }
        public int? Stock { get => Inventory; set => Inventory = value; }
        public string? Location { get; set; }
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? CreatedTime { get; set; }
        public int? DeptID { get; set; }
        public string? DeptCode { get; set; }
        public string? DeptName { get; set; }
        public int? FACID { get; set; }
        public bool IsLowStock => Inventory <= MinStock;
        public string DisplayName => $"{PartCode} — {PartName}";

    }
}
