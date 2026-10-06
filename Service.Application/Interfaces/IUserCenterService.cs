using Service.Application.DTOs.UserCenter;

namespace Service.Application.Interfaces
{
    public interface IUserCenterService
    {
        Task<UserCentersGetDto> GetAssignedAsync(int userId, CancellationToken cancellationToken = default);
        Task<MyCentersGetDto> GetAccessibleAsync(int userId, CancellationToken cancellationToken = default);
        Task<UserCentersGetDto> SetCentersAsync(int userId, UserCentersPutDto putDto, CancellationToken cancellationToken = default);
        Task<UserCentersGetDto> SetAllCentersAccessAsync(int userId, bool hasAllCentersAccess, CancellationToken cancellationToken = default);
    }
}
