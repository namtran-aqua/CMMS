using System;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartItemDto
    {
        public int ItemID { get; set; }
        public int SPID { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public int ImportID { get; set; }
        public string? ImportCode { get; set; }
        public int ImportDetailID { get; set; }
        public bool HasCode { get; set; }
        public string? SerialCode { get; set; }
        public int Quantity { get; set; }
        public int RemainingQuantity { get; set; }
        public DateTime ImportDate { get; set; }
        public int DaysInStock { get; set; }
        public string Status { get; set; } = "Available"; // Available, Reserved, Installed, Scrapped, Returned, Lost
        public int? FACID { get; set; }
        public int? DeptID { get; set; }
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }
        public decimal? Price { get; set; }
    }
}
