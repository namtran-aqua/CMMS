using System;
using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class ExportOrderDto
    {
        public int ExportID { get; set; }
        public string ExportCode { get; set; } = string.Empty;
        public int? MovementTypeID { get; set; }
        public string? MovementTypeName { get; set; }
        public DateTime ExportDate { get; set; } = DateTime.Now;
        public int? FACID { get; set; }
        public string Status { get; set; } = "Completed"; // Draft, Completed, Cancelled, Reversed, etc.
        public Guid? CreateBy { get; set; }
        public string? CreateUser { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.Now;
        public Guid? UpdateBy { get; set; }
        public DateTime? UpdateAt { get; set; }
        
        public List<ExportOrderDetailDto> Details { get; set; } = new();
        public List<string> AttachmentUrls { get; set; } = new();
    }

    public class ExportOrderDetailDto
    {
        public int DetailID { get; set; }
        public int ExportID { get; set; }
        public int SPID { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public bool HasCode { get; set; }
        public string? SerialCode { get; set; }
        public int Quantity { get; set; }
    }
}
