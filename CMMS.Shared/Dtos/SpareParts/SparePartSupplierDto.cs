using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartSupplierDto
    {
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? CreateDate { get; set; }
        public Guid? CreateBy { get; set; }
    }
}
