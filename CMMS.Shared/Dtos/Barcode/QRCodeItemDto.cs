using System.Collections.Generic;

namespace CMMS.Shared.Dtos.Barcode
{
    public class QRCodeItemDto
    {
        public int Id { get; set; } // EQID, SPID, or ItemID
        public string? EntityType { get; set; } // "Equipment", "CodedSP", "NonCodedSP"
        public string? BarcodeId { get; set; } // Can be null if not generated
        public string? Code { get; set; } // EquipmentCode or PartCode
        public string? Name { get; set; } // EquipmentName or PartName
        public string? Serial { get; set; } // For Coded SP or Equipment
        public string? Status { get; set; }
    }

    public class GenerateBarcodeRequestDto
    {
        public List<QRCodeItemDto> Items { get; set; }
    }

    public class ExportPdfRequestDto
    {
        public List<QRCodeItemDto> Items { get; set; }
    }
}
