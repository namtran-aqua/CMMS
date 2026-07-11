using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Maintenance
{
    public class MaintenanceSparePartDto
    {
        public int SPID { get; set; }
        public string? PartName { get; set; }
        public string? PartCode { get; set; }
        public string? Unit { get; set; }
        public int Qty { get; set; }
        public int? Inventory { get; set; }
    }
}
