using Service.Application.DTOs.Product;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IProductService : IBaseService<Product, ProductGetDto, ProductPostDto, ProductPutDto>
    {
        Task<PagedList<ProductGetDto>> GetByCenterAsync(int idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
