using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Equipment
{
    public class DepartmentDto
    {

        public int? DeptID { get; set; }
        public int? FACID { get; set; }
        public string? FACCode { get; set; }
        public string? FACName { get; set; }
        public string? FACFullName { get; set; }
        public string? DeptCode { get; set; }
        public string? DeptName { get; set; }
        public string? DeptFullName { get; set; }
        public string? FactoryDept { get; set; }
        public string? HODWD { get; set; }

    }
}
