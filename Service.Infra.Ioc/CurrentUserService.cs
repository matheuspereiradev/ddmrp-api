using Microsoft.AspNetCore.Http;
using Service.Domain.Account;

namespace Service.Infra.Ioc
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId => _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
    }
}
