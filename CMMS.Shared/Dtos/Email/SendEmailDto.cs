using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.Email
{
    public class SendEmailDto
    {
        public string RequestId { get; set; }
        public string ToAdress { get; set; }
        public string Subject { get; set; }
        public StringBuilder Body { get; set; }
    }
}
