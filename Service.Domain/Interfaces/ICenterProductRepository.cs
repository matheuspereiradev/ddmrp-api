using Service.Domain.Entities;

namespace Service.Domain.Interfaces
{
    public interface ICenterProductRepository : IBaseRepository<CenterProduct>
    {
        Task<CenterProduct> GetByProductAndCenterAsync(int idProduct, int idCenter, CancellationToken cancellationToken = default);
    }
}
