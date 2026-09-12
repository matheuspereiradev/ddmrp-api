using Service.Application.DTOs.Product;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IProductService : IBaseService<Product, ProductGetDto, ProductPostDto, ProductPutDto>
    {
    }
}
