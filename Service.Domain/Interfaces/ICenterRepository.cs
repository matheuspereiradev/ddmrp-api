using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface ICenterRepository : IBaseRepository<Center>
    {
        Task<Dictionary<string, int>> GetIdsByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
    }
}
