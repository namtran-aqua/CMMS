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
}
