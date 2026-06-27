using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.User
{
    public class CurrentUserInfo
    {
        public Guid UserId { get; set; }
        public string WorkDayId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? FACID { get; set; }
        public int? DeptID { get; set; }
        public int? LocID { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }
}
