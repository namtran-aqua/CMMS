using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace CMMS.Server.Infrastructure.Authorization
{
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public RequirePermissionAttribute(string permission)
        {
            Policy = $"Permission:{permission}";
        }
    }
}
