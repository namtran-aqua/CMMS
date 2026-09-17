namespace CMMS.Shared.Dtos.Scanner
{
    public class ScannerItemDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty; // "Equipment", "SparePart", "CodedSparePart"
        public string BarcodeId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Serial { get; set; }
        
        public string? Specification { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinStock { get; set; }
        public string? Unit { get; set; }
        public decimal Price { get; set; }
        public string? Location { get; set; }
        public string? Supplier { get; set; }
        public string? Department { get; set; }
        public int AgeDays { get; set; }
        public string? ImportDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
