using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class AdjustStockRequestDto
    {
        public int SPID { get; set; }
        public string Type { get; set; } // IN | OUT
        public int Qty { get; set; }
        public string? RefCode { get; set; }
        public string? Note { get; set; }
    }
}
