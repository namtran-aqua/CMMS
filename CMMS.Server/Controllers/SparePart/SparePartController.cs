using CMMS.Server.Services.SparePartService;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.SpareParts;
using CMMS.Shared.Dtos.Common;
using CMMS.Shared.Dtos.User;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMMS.Server.Controllers.SparePart
{
    [ApiController]
    [Route("api/[controller]")]
    public partial class SparePartController : ControllerBase
    {
        private readonly ISparePartService _service;
        private readonly IUserService _userService;

        public SparePartController(ISparePartService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
        }

        private async Task<UserDto?> GetCurrentUserAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return null;
            return await _userService.GetCurrentUserAsync(userId);
        }
    }
}