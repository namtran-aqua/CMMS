using System;
using System.ComponentModel.DataAnnotations;

namespace CMMS.Shared.Dtos.User
{
    public class UpdateUserRequest
    {
        [Required(ErrorMessage = "User is required.")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Factory is required.")]
        public int FacId { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public int DeptId { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        public int RoleId { get; set; }

        public bool IsActive { get; set; }
    }
}
