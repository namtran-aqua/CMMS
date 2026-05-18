using CMMS.Server.Services.DashBoardService;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.Server.Controllers.DashBoards
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashBoardController : ControllerBase
    {
        private readonly IDashBoardService _dashBoardService;

        public DashBoardController(IDashBoardService dashBoardService)
        {
            _dashBoardService = dashBoardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashBoard()
        {
            var result = await _dashBoardService.GetDashBoard();
            return Ok(result);
        }
    }
}
