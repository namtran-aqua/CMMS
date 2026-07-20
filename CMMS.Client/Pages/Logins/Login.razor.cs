using CMMS.Shared.Dtos.AuthModels;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace CMMS.Client.Pages.Logins;

public enum LoginScreenState
{
    Login,
    ForgotPassword,
    VerifyOtp,
    ResetPassword
}

public partial class Login
{
    [Inject] private HttpClient Http { get; set; }
    [Inject] private CustomAuthenticationStateProvider AuthProvider { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    [Inject] private ISessionStorageService _sessionStorage { get; set; }

    // Screen State
    private LoginScreenState currentScreenState = LoginScreenState.Login;
    private bool loading = false;

    // Login Fields
    private string username { get; set; } = "";
    private string password { get; set; } = "";
    private bool showPassword = false;

    // Forgot Password Fields
    private string forgotWorkDayId { get; set; } = "";
    private string forgotEmail { get; set; } = "";

    // Verify OTP Fields
    private string otpCode { get; set; } = "";

    // Reset Password Fields
    private string resetToken { get; set; } = "";
    private string newPassword { get; set; } = "";
    private string confirmPassword { get; set; } = "";
    private bool showNewPassword = false;
    private bool showConfirmPassword = false;

    private async Task DoLogin()
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            Message.Error("username cannot be blank");
            return;
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            Message.Error("password cannot be blank");
            return;
        }

        loading = true;
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/login", new LoginRequest
            {
                UserName = username,
                Password = password
            });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Message.Error("Incorrect Account or Password, Please Check Again !");
                return;
            }
            if (!response.IsSuccessStatusCode)
            {
                Message.Error($"Server error: {(int)response.StatusCode}");
                return;
            }
            var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            if (content == null || !content.ContainsKey("token"))
            {
                Message.Error("Invalid response from server.");
                return;
            }
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(content["token"]);
            var claims = token.Claims.ToList();

            await _sessionStorage.SetItemAsync("authToken", content["token"]);
            AuthProvider.MarkUserAsAuthenticated(username, claims);

            var baseUri = Nav.BaseUri.TrimEnd('/');
            Nav.NavigateTo($"{baseUri}/");
        }
        catch (Exception ex)
        {
            Message.Error($"An error occurred: {ex.Message}");
        }
        finally
        {
            loading = false;
        }
    }

    private void TogglePassword()
    {
        showPassword = !showPassword;
    }

    private void ToggleNewPassword()
    {
        showNewPassword = !showNewPassword;
    }

    private void ToggleConfirmPassword()
    {
        showConfirmPassword = !showConfirmPassword;
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            if (currentScreenState == LoginScreenState.Login)
            {
                await DoLogin();
            }
            else if (currentScreenState == LoginScreenState.ForgotPassword)
            {
                await SendOtp();
            }
            else if (currentScreenState == LoginScreenState.VerifyOtp)
            {
                await VerifyOtp();
            }
            else if (currentScreenState == LoginScreenState.ResetPassword)
            {
                await ResetPassword();
            }
        }
    }

    private void GoToForgotPassword()
    {
        forgotWorkDayId = "";
        forgotEmail = "";
        currentScreenState = LoginScreenState.ForgotPassword;
    }

    private void GoToLogin()
    {
        currentScreenState = LoginScreenState.Login;
    }

    private async Task SendOtp()
    {
        if (string.IsNullOrWhiteSpace(forgotWorkDayId))
        {
            Message.Error("WorkDay ID cannot be blank");
            return;
        }
        if (string.IsNullOrWhiteSpace(forgotEmail))
        {
            Message.Error("Email cannot be blank");
            return;
        }

        loading = true;
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/forgot-password", new ForgotPasswordRequest
            {
                WorkDayId = forgotWorkDayId,
                Email = forgotEmail
            });

            if (response.IsSuccessStatusCode)
            {
                Message.Success("A verification code has been sent to your email.");
                otpCode = "";
                currentScreenState = LoginScreenState.VerifyOtp;
            }
            else
            {
                await HandleApiError(response);
            }
        }
        catch (Exception ex)
        {
            Message.Error($"An error occurred: {ex.Message}");
        }
        finally
        {
            loading = false;
        }
    }

    private async Task VerifyOtp()
    {
        if (string.IsNullOrWhiteSpace(otpCode))
        {
            Message.Error("OTP code cannot be blank");
            return;
        }

        loading = true;
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/verify-otp", new VerifyOtpRequest
            {
                WorkDayId = forgotWorkDayId,
                Otp = otpCode
            });

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (content != null && content.TryGetValue("resetToken", out string token))
                {
                    resetToken = token;
                    newPassword = "";
                    confirmPassword = "";
                    currentScreenState = LoginScreenState.ResetPassword;
                }
                else
                {
                    Message.Error("Invalid response from server.");
                }
            }
            else
            {
                await HandleApiError(response);
            }
        }
        catch (Exception ex)
        {
            Message.Error($"An error occurred: {ex.Message}");
        }
        finally
        {
            loading = false;
        }
    }

    private async Task ResetPassword()
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            Message.Error("New password cannot be blank");
            return;
        }
        if (newPassword.Length < 6)
        {
            Message.Error("Password must be at least 6 characters long.");
            return;
        }
        if (newPassword != confirmPassword)
        {
            Message.Error("Confirm password does not match the new password.");
            return;
        }

        loading = true;
        try
        {
            var response = await Http.PostAsJsonAsync("api/auth/reset-password", new ResetPasswordRequest
            {
                ResetToken = resetToken,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            });

            if (response.IsSuccessStatusCode)
            {
                Message.Success("Password has been reset successfully.");
                currentScreenState = LoginScreenState.Login;
                username = forgotWorkDayId;
                password = "";
            }
            else
            {
                await HandleApiError(response);
            }
        }
        catch (Exception ex)
        {
            Message.Error($"An error occurred: {ex.Message}");
        }
        finally
        {
            loading = false;
        }
    }

    private async Task HandleApiError(HttpResponseMessage response)
    {
        try
        {
            var err = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (err != null && err.TryGetValue("message", out string msg))
            {
                Message.Error(msg);
                return;
            }
        }
        catch { }
        Message.Error($"Error: {response.ReasonPhrase}");
    }
}
