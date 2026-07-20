using CMMS.Server.Services.UserService;
using CMMS.Shared.Dtos.AuthModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    public AuthController(IUserService userService)
    {
        _userService = userService;
    }
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _userService.LoginAsync(request);
        if (result == null)
            return Unauthorized();

        return Ok(new Dictionary<string, string> {
            { "token", result.Token }
        });
    }
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePassRequest request)
    {
        try
        {
            var success = await _userService.ChangePasswordAsync(request);
            if (success)
                return Ok(new { message = "Password changed successfully." });
            return BadRequest(new { message = "Password change failed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        try
        {
            var success = await _userService.SendOtpAsync(request);
            if (success)
                return Ok(new { message = "A verification code has been sent to your email." });
            return BadRequest(new { message = "Failed to send OTP." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        try
        {
            var token = await _userService.VerifyOtpAsync(request);
            return Ok(new { resetToken = token });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        try
        {
            var success = await _userService.ResetPasswordWithTokenAsync(request);
            if (success)
                return Ok(new { message = "Password has been reset successfully." });
            return BadRequest(new { message = "Password reset failed." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}