using Service.Application.DTOs.CenterProduct;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface ICenterProductService : IBaseService<CenterProduct, CenterProductGetDto, CenterProductPostDto, CenterProductPutDto>
    {
    }
}
