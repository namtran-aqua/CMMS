using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.User
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string WorkDayId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; } = string.Empty;
        public Guid? ManagerId { get; set; }
        public string? PhoneNumber { get; set; }
        public Guid? GroupId { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public string? UpdateBy { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public Guid? FactoryId { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsActive { get; set; }
        public int FlowApproval { get; set; }
    }
}
