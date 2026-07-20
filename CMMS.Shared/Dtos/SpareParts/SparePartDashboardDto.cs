using System;
using System.Collections.Generic;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartDashboardDto
    {
        public int TotalSKU { get; set; }
        public int CurrentInventory { get; set; }
        public decimal InventoryValue { get; set; }
        public int ImportThisMonth { get; set; }
        public int ExportThisMonth { get; set; }
        public int LowStockSKU { get; set; }
        public int ZeroStockSKU { get; set; }
        
        public List<TopUsedPartDto> TopUsedParts { get; set; } = new();
        public List<AgingDistributionDto> AgingDistribution { get; set; } = new();
        public List<TopVendorDto> TopVendors { get; set; } = new();
    }

    public class TopUsedPartDto
    {
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public int QuantityUsed { get; set; }
    }

    public class AgingDistributionDto
    {
        public string Range { get; set; } = string.Empty; // "0-30 days", "30-60 days", etc.
        public int Quantity { get; set; }
        public decimal Value { get; set; }
    }

    public class TopVendorDto
    {
        public string? VendorName { get; set; }
        public int TransactionsCount { get; set; }
        public decimal TotalValue { get; set; }
    }
}
