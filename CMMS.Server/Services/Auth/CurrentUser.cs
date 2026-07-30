using CMMS.Shared.Constants;
using System.Security.Claims;

namespace CMMS.Server.Services.Auth
{
    public interface ICurrentUser
    {
        Guid UserId { get; }
        string WorkDayId { get; }
        string FullName { get; }
        int? RoleId { get; }
        int? FacId { get; }
        int? DeptId { get; }
        int? LocId { get; }

        bool IsAdmin { get; }
        bool IsIT { get; }
        bool HasGlobalView { get; }
    }

    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid UserId 
        { 
            get 
            {
                var val = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                return Guid.TryParse(val, out var id) ? id : Guid.Empty;
            } 
        }

        public string WorkDayId => User?.FindFirstValue(ClaimTypes.SerialNumber) ?? string.Empty;
        
        public string FullName => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        public int? RoleId
        {
            get
            {
                var val = User?.FindFirstValue("RoleID");
                return int.TryParse(val, out var roleId) ? roleId : null;
            }
        }

        public int? FacId
        {
            get
            {
                var val = User?.FindFirstValue("FACID");
                return int.TryParse(val, out var facId) ? facId : null;
            }
        }

        public int? DeptId
        {
            get
            {
                var val = User?.FindFirstValue("DeptID");
                return int.TryParse(val, out var deptId) ? deptId : null;
            }
        }

        public int? LocId
        {
            get
            {
                var val = User?.FindFirstValue("LocID");
                return int.TryParse(val, out var locId) ? locId : null;
            }
        }

        public bool IsAdmin => RoleId == SystemRoles.Admin;
        
        public bool IsIT => RoleId == SystemRoles.IT;
        
        public bool HasGlobalView => IsAdmin || IsIT;
    }
}
