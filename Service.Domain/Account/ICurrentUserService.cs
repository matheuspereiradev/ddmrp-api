namespace Service.Domain.Account
{
    public interface ICurrentUserService
    {
        int UserId { get; }

        Task<IReadOnlyCollection<int>?> GetAllowedCenterIdsAsync(CancellationToken cancellationToken = default);
    }
}
