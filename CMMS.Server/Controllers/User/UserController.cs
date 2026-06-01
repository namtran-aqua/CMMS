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
}