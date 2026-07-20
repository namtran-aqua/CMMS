using System;
using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class ImportOrderDto
    {
        public int ImportID { get; set; }
        public string ImportCode { get; set; } = string.Empty;
        public string? PONumber { get; set; }
        public int? VendorID { get; set; }
        public string? VendorName { get; set; }
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public int? FACID { get; set; }
        public string Status { get; set; } = "Completed"; // Draft, Completed, Cancelled, Reversed, etc.
        public Guid? CreateBy { get; set; }
        public string? CreateUser { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public Guid? UpdateBy { get; set; }
        public DateTime? UpdateAt { get; set; }
        
        public List<ImportOrderDetailDto> Details { get; set; } = new();
        public List<string> AttachmentUrls { get; set; } = new();
    }

    public class ImportOrderDetailDto
    {
        public int DetailID { get; set; }
        public int ImportID { get; set; }
        public int SPID { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public bool HasCode { get; set; }
        public string? SerialCode { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
    }
}
