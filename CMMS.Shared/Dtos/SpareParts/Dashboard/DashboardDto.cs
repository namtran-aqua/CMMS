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
        public CodedRatioDto CodedRatio { get; set; } = new();
        public StockStatusDto StockStatus { get; set; } = new();
        public List<InOutTrendDto> InOutTrends { get; set; } = new();
        public List<AgingDistributionDto> MovementAging { get; set; } = new();
        public List<TopTransactionDto> TopImported { get; set; } = new();
        public List<TopTransactionDto> TopExported { get; set; } = new();
        public List<LowStockByLocationDto> LowStockByLocation { get; set; } = new();
        public List<PartAgingDto> AgingReport { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }

    public class SummaryDto
    {
        public decimal TotalInventoryQuantity { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public decimal ImportThisMonth { get; set; }
        public double? ImportChangePercentage { get; set; }
        public decimal ExportThisMonth { get; set; }
        public double? ExportChangePercentage { get; set; }
    }

    public class CodedRatioDto
    {
        public int CodedCount { get; set; }
        public int NonCodedCount { get; set; }
    }

    public class StockStatusDto
    {
        public int HealthyStock { get; set; }
        public int LowStock { get; set; }
        public int OutOfStock { get; set; }
    }

    public class InOutTrendDto
    {
        public string Month { get; set; } = string.Empty; // e.g. "2026-08"
        public decimal ImportQuantity { get; set; }
        public decimal ExportQuantity { get; set; }
    }

    public class AgingDistributionDto
    {
        public string Range { get; set; } = string.Empty; // "0-30", "31-60", "61-90", ">90", "No Movement"
        public decimal Quantity { get; set; }
    }

    public class TopTransactionDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class LowStockByLocationDto
    {
        public string Location { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PartAgingDto
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public DateTime? LastMovementDate { get; set; }
        public int? AgeDays { get; set; }
    }

    // Legacy DTOs kept to prevent build errors in unused services
    public class InventoryKpiDto
    {
        public decimal InventoryValue { get; set; }
        public decimal InventoryTurnover { get; set; }
        public decimal DeadStockValue { get; set; }
        public decimal StockAccuracy { get; set; }
        public decimal FillRate { get; set; }
    }

    public class AlertDto
    {
        public int Critical { get; set; }
        public int Warning { get; set; }
        public int Info { get; set; }
        public int Normal { get; set; }
    }
}
