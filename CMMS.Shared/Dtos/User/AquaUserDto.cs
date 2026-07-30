using System;

namespace CMMS.Shared.Dtos.User
{
    public class AquaUserDto
    {
        public Guid Id { get; set; }
        public string WorkDayId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
