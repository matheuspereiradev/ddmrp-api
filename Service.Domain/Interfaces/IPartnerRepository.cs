using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IPartnerRepository : IBaseRepository<Partner>
    {
        Task<Dictionary<string, int>> GetIdsByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
    }
}
