using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface IProductRepository : IBaseRepository<Product>
    {
        Task<Dictionary<string, int>> GetIdsByReferencesAsync(IEnumerable<string> references, CancellationToken cancellationToken = default);
    }
}
