using Crud.Exceptions;
using Crud.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Crud.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            var isAuthenticated = user?.Identity?.IsAuthenticated;

            var claims = user?.Claims
                .Select(c => new
                {
                    c.Type,
                    c.Value
                })
                .ToList();

            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            throw new UnauthorizedException();
        }

        public string GetCurrentUsername()
        {
       var username = _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
            if (string.IsNullOrEmpty(username)) {
                throw new UnauthorizedException();
            }
            return username ?? string.Empty;
        }

        public string GetCurrentUserRole()
        {
            var role = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
            return role ?? string.Empty;
        }

        public string GetEmail()
        {
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
            return email ?? string.Empty;
        }
        private string GetRequiredClaim(string claimType)
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
            if (string.IsNullOrEmpty(claim))
            {
                throw new UnauthorizedException();
            }
            return claim;
        }
            

    }
}
