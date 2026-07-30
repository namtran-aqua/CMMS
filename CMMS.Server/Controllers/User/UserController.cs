using Microsoft.AspNetCore.Mvc;
using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.User;

[ApiController] 
[Route("api/[controller]")]

public class UserController : ControllerBase
{
    private readonly IUserService _service;
    public UserController(IUserService service)
    {
        _service = service;
    }
    [HttpGet("get-all")]
    public async Task<List<UserDto>> GetAll()
    {
        return await _service.GetUsersAsync();
    }
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            Console.WriteLine("API HIT ✅");
            var data = await _service.GetUsersAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR ❌: " + ex.ToString());
            return StatusCode(500, ex.Message);
        }
    }
    [HttpGet("get-currentUser/{userId}")]
    public async Task<UserDto?> GetCurrentUser(Guid userId)
    {
        var currentUser = await _service.GetCurrentUserAsync(userId);
        if (currentUser == null)
            return new UserDto();

        return currentUser;
    }

    [HttpGet("aqua-users")]
    public async Task<IActionResult> GetAquaUsers([FromQuery] string keyword = "")
    {
        try
        {
            var data = await _service.GetAquaUsersAsync(keyword);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    private async Task<UserDto> GetApiUserAsync()
    {
        var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            throw new Exception("Unauthorized");

        var user = await _service.GetCurrentUserAsync(userId);
        if (user == null)
            throw new Exception("Unauthorized");
            
        return user;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var currentUser = await GetApiUserAsync();
            var result = await _service.CreateUserAsync(request, currentUser);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request)
    {
        try
        {
            var currentUser = await GetApiUserAsync();
            var result = await _service.UpdateUserAsync(request, currentUser);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/disable")]
    public async Task<IActionResult> DisableUser(Guid id)
    {
        try
        {
            var currentUser = await GetApiUserAsync();
            var result = await _service.DisableUserAsync(id, currentUser);
            return Ok(new { success = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("roles")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public IActionResult GetRoles()
    {
        var roles = new List<object>
        {
            new { Id = 1, Name = "Manager" },
            new { Id = 2, Name = "User" },
            new { Id = 3, Name = "Admin" },
            new { Id = 4, Name = "IT" }
        };
        return Ok(roles);
    }
}