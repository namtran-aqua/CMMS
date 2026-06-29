using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Maintenance.Attachments
{
    public class AttachmentDto
    {
        public Guid Id { get; set; }
        public long MTID { get; set; }
        public string FilePath { get; set; }
        public string FileExtend { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
