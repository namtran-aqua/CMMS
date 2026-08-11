using System;
using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts.Dashboard
{
    public enum MovementType
    {
        Import = 1,
        Export = 2,
        Adjustment = 3,
        Transfer = 4,
        Maintenance = 5,
        Scrap = 6,
        Return = 7
    }

    public class DashboardFilterDto
    {
        public int? FactoryId { get; set; }
        public int? SectionId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public MovementType? MovementType { get; set; }
    }

    public class DashboardDto
    {
        public SummaryDto Summary { get; set; } = new();
        public InventoryKpiDto Kpi { get; set; } = new();
        public AlertDto Alerts { get; set; } = new();
        public List<TrendDto> Trends { get; set; } = new();
        public List<TopConsumedDto> TopConsumed { get; set; } = new();
        public List<RecentMovementDto> RecentMovements { get; set; } = new();
        public List<CategoryValueDto> CategoryValues { get; set; } = new();
    }

    public class SummaryDto
    {
        public int TotalSKUs { get; set; }
        public int InStockSKUs { get; set; }
        public int LowStockSKUs { get; set; }
        public int ZeroStockSKUs { get; set; }
    }

    public class InventoryKpiDto
    {
        public decimal InventoryValue { get; set; }
        public decimal InventoryTurnover { get; set; } // Hệ số quay vòng
        public decimal DeadStockValue { get; set; } // Hàng tồn chậm luân chuyển
        public decimal StockAccuracy { get; set; } // Độ chính xác tồn kho
        public decimal FillRate { get; set; } // Tỷ lệ đáp ứng
    }

    public class AlertDto
    {
        public int Critical { get; set; } // Ví dụ: Hết hàng
        public int Warning { get; set; }  // Ví dụ: Sắp hết
        public int Info { get; set; }     // Ví dụ: Có biến động
        public int Normal { get; set; }   // Ví dụ: Tồn kho dư thừa
    }

    public class TrendDto
    {
        public string Period { get; set; } = string.Empty; // e.g. "2026-01"
        public MovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal Value { get; set; }
    }

    public class TopConsumedDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Value { get; set; }
        public decimal Percentage { get; set; }
        public string Trend { get; set; } = string.Empty; // "Up" or "Down"
    }

    public class RecentMovementDto
    {
        public string TransactionCode { get; set; } = string.Empty;
        public string PartCode { get; set; } = string.Empty;
        public MovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public DateTime Date { get; set; }
    }

    public class CategoryValueDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
