using Microsoft.AspNetCore.Http;
using Service.Domain.Account;
using Service.Domain.Interfaces;

namespace Service.Infra.Ioc
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserCenterRepository _userCenterRepository;
        private bool _allowedCenterIdsLoaded;
        private IReadOnlyCollection<int>? _allowedCenterIds;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserCenterRepository userCenterRepository)
        {
            _httpContextAccessor = httpContextAccessor;
            _userCenterRepository = userCenterRepository;
        }

        public int UserId => _httpContextAccessor.HttpContext?.User.GetUserId()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        public async Task<IReadOnlyCollection<int>?> GetAllowedCenterIdsAsync(CancellationToken cancellationToken = default)
        {
            if (!_allowedCenterIdsLoaded)
            {
                _allowedCenterIds = await _userCenterRepository.GetAllowedCenterIdsAsync(UserId, cancellationToken);
                _allowedCenterIdsLoaded = true;
            }

            return _allowedCenterIds;
        }
    }
}
