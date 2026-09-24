using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Service.Infra.Ioc
{
    public static class ClaimsPrincipalExtension
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("id");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            throw new Exception("User ID claim not found or invalid.");
        }

        public static int GetRoleId(this ClaimsPrincipal user)
        {
            var roleIdClaim = user.FindFirst("role");
            if (roleIdClaim != null && int.TryParse(roleIdClaim.Value, out int roleId))
            {
                return roleId;
            }
            throw new Exception("Role ID claim not found or invalid.");
        }
    }
}
