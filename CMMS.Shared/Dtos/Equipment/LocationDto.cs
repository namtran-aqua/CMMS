using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Equipment
{
    public class LocationDto
    {
        public int? LocID { get; set; }
        public int? DeptID { get; set; }
        public int? FACID { get; set; }
        public string? LocName { get; set; }
        public string? LocCode { get; set; }
        public string? LocManager { get; set; }
    }
}
