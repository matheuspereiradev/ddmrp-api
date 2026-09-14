using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Domain.Interfaces
{
    public interface IProductRepository : IBaseRepository<Product>
    {
        Task<Dictionary<string, int>> GetIdsByReferencesAsync(IEnumerable<string> references, CancellationToken cancellationToken = default);
        Task<PagedList<Product>> GetByCenterAsync(int idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
