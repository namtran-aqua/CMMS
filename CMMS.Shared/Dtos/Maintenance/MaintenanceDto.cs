using CMMS.Shared.Dtos.Maintenance.Attachments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Maintenance
{
    public class MaintenanceDto
    {
        public long MTID { get; set; }
        public int EQID { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateTime { get; set; }
        public int StsMainID { get; set; }
        public DateTime? MaintDate { get; set; }
        public int VendorID { get; set; }
        public decimal? MaintPrice { get; set; }
        public string? PICID { get; set; }
        public string? MaintPIC { get; set; }
        public string? MaintDescription { get; set; }
        public string? MaintNote { get; set; }
        public bool IsEQActive { get; set; } = true;
        public List<MaintenanceDto> Items { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();
        public string? WorkDayId { get; set; }
    }
}
