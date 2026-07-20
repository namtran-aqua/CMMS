using System;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartMonthlyPeriodDto
    {
        public int PeriodID { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int SPID { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public int OpeningQty { get; set; }
        public decimal OpeningValue { get; set; }
        public int ImportQty { get; set; }
        public int ExportQty { get; set; }
        public int AdjustmentQty { get; set; }
        public int ClosingQty { get; set; }
        public decimal ClosingValue { get; set; }
        public DateTime ClosingDate { get; set; }
        public string? CreateUser { get; set; }
    }
}
