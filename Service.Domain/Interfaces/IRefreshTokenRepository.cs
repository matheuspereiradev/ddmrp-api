using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    }
}
