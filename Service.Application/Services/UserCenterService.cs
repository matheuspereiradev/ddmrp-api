using Service.Application.DTOs.UserCenter;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class UserCenterService : IUserCenterService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserCenterRepository _userCenterRepository;
        private readonly ICenterRepository _centerRepository;

        public UserCenterService(
            IUserRepository userRepository,
            IUserCenterRepository userCenterRepository,
            ICenterRepository centerRepository)
        {
            _userRepository = userRepository;
            _userCenterRepository = userCenterRepository;
            _centerRepository = centerRepository;
        }

        public async Task<UserCentersGetDto> GetAssignedAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            var centers = await _userCenterRepository.GetAssignedCentersAsync(userId, cancellationToken);
            return ToDto(user, centers);
        }

        public async Task<MyCentersGetDto> GetAccessibleAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            var centerIds = await _userCenterRepository.GetAccessibleCenterIdsAsync(userId, cancellationToken);
            return new MyCentersGetDto
            {
                IdUser = user.Id,
                HasAllCentersAccess = user.HasAllCentersAccess,
                Centers = centerIds
            };
        }

        public async Task<UserCentersGetDto> SetCentersAsync(int userId, UserCentersPutDto putDto, CancellationToken cancellationToken = default)
        {
            await GetUserAsync(userId, cancellationToken);

            var centerIds = putDto.CenterIds.Distinct().ToList();
            var existingIds = await _centerRepository.GetExistingIdsAsync(centerIds, cancellationToken);
            var missingIds = centerIds.Where(id => !existingIds.Contains(id)).ToList();
            if (missingIds.Count > 0)
                throw new BadRequestException($"Center(s) not found: {string.Join(", ", missingIds)}.");

            await _userCenterRepository.ReplaceAsync(userId, centerIds, cancellationToken);
            return await GetAssignedAsync(userId, cancellationToken);
        }

        public async Task<UserCentersGetDto> SetAllCentersAccessAsync(int userId, bool hasAllCentersAccess, CancellationToken cancellationToken = default)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            user.HasAllCentersAccess = hasAllCentersAccess;
            await _userRepository.UpdateAsync(user, cancellationToken);
            return await GetAssignedAsync(userId, cancellationToken);
        }

        private async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new NotFoundException("User not found");
            return user;
        }

        private static UserCentersGetDto ToDto(User user, List<Center> centers) => new()
        {
            IdUser = user.Id,
            HasAllCentersAccess = user.HasAllCentersAccess,
            Centers = centers.Select(c => c.ToGetDto()).ToList()
        };
    }
}
