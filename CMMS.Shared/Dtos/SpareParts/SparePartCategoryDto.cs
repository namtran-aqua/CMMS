using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartCategoryDto
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public DateTime? CreateDate { get; set; }
        public Guid? CreateBy { get; set; }
    }
}
