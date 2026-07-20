using System;
using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class CreateAdjustOrderDto
    {
        public DateTime AdjustDate { get; set; } = DateTime.Now;
        public int? FACID { get; set; }
        public string? Note { get; set; }
        public List<string> AttachmentUrls { get; set; } = new();
        public List<AdjustOrderDetailDto> Lines { get; set; } = new();
    }
}
