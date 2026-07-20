using System;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class AdjustOrderDetailDto
    {
        public int DetailID { get; set; }
        public int AdjustID { get; set; }
        public int SPID { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public string Type { get; set; } = "IN"; // IN | OUT
        public bool HasCode { get; set; }
        public string? SerialCode { get; set; }
        public int Quantity { get; set; }
        public int BeforeQty { get; set; }
        public int AfterQty { get; set; }
        public int? ItemID { get; set; }
    }
}
