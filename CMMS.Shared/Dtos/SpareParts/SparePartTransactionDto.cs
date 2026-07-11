using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.SpareParts
{
    public class SparePartTransactionDto
    {
        public int TransID { get; set; }
        public int? FACID { get; set; }
        public int SPID { get; set; }
        public string? PartCode { get; set; }
        public string? PartName { get; set; }
        public string Type { get; set; } // IN | OUT | MAINTENANCE
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public int? EQID { get; set; }
        public string? Equipment { get; set; }
        public string? Note { get; set; }
        public Guid? CreateBy { get; set; }
        public string? CreateUser { get; set; }
        public DateTime CreateDate { get; set; }
        public string? RefCode { get; set; }
        public int? DeptID { get; set; }


        //// Aliased properties to match database columns and frontend bindings
        //public long TxID { get => TransID; set => TransID = value; }
        //public int Qty { get => Quantity; set => Quantity = value; }
        //public DateTime TxDate { get => Date; set => Date = value; }
    }
}
