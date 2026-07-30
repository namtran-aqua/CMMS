using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Equipment
{
    public class FactoryDto
    {
        public int? FACID { get; set; }
        public string? FACCode { get; set; }
        public string? FACName { get; set; }
        public string? FACFullName { get; set; }
    }
}
