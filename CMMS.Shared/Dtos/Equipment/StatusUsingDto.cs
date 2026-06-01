using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Equipment
{
    public class StatusUsingDto
    {
        public int? StsUseID { get; set; }
        public string? StsType { get; set; }
        public string? StsUseName { get; set; }
        public string? StsUseNote { get; set; }
    }
}
