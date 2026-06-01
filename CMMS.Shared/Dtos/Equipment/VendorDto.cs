using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Equipment
{
    public class VendorDto
    {
        public int? VendorID { get; set; }
        public string? VendorName { get; set; }
        public string? VendorCode { get; set; }
        public string? VendorAddress { get; set; }
        public string? VendorEmail { get; set; }
        public string? VendorPhone { get; set; }
        public string? VendorContact { get; set; }
        public string? VendorNote { get; set; }
        public string? VendorBankName { get; set; }
    }

}
