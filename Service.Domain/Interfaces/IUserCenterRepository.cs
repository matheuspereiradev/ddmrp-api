using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IUserCenterRepository
    {
        Task<IReadOnlyCollection<int>?> GetAllowedCenterIdsAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<Center>> GetAssignedCentersAsync(int userId, CancellationToken cancellationToken = default);
        Task<List<int>> GetAccessibleCenterIdsAsync(int userId, CancellationToken cancellationToken = default);
        Task ReplaceAsync(int userId, IReadOnlyCollection<int> centerIds, CancellationToken cancellationToken = default);
    }
}
