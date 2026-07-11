using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class MaintenanceExportRequestDto
    {
        public int? EQID { get; set; }
        public string Equipment { get; set; }
        public string? RequestedBy { get; set; }
        public string? Note { get; set; }
        public List<MaintenanceExportLineDto> Lines { get; set; } = new();
    }
}
