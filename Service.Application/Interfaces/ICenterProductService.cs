using Service.Application.DTOs.CenterProduct;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface ICenterProductService : IBaseService<CenterProduct, CenterProductGetDto, CenterProductPostDto, CenterProductPutDto>
    {
        Task<CenterProductGetDto> SetAllocationGroupAsync(int id, int? idAllocationGroup, CancellationToken cancellationToken = default);
        Task<CenterProductGetDto> SetTagAsync(int id, int? idTag, CancellationToken cancellationToken = default);
        Task<CenterProductGetDto> SetReasonAsync(int id, int? idReason, CancellationToken cancellationToken = default);
    }
}
