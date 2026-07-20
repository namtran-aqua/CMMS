using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Shared.Dtos.AuthModels
{
    public class LoginRequest
    {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public DateTime Expiration { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string WorkDayId { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class VerifyOtpRequest
    {
        public string WorkDayId { get; set; } = "";
        public string Otp { get; set; } = "";
    }

    public class ResetPasswordRequest
    {
        public string ResetToken { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
