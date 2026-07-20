using System;
using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class AdjustOrderDto
    {
        public int AdjustID { get; set; }
        public string AdjustCode { get; set; } = string.Empty;
        public DateTime AdjustDate { get; set; }
        public int? FACID { get; set; }
        public string Status { get; set; } = "Completed";
        public string? Note { get; set; }
        public Guid? CreateBy { get; set; }
        public DateTime CreateAt { get; set; }
        public Guid? UpdateBy { get; set; }
        public DateTime? UpdateAt { get; set; }

        // Extracted audit/summary fields
        public string? CreateUser { get; set; } // Username / WorkDayID
        public List<string> AttachmentUrls { get; set; } = new();
        public int TotalLines { get; set; }
        public int NetAdjustment { get; set; } // Sum of (+Qty for IN, -Qty for OUT)

        public List<AdjustOrderDetailDto> Lines { get; set; } = new();
    }
}
