using Service.Application.DTOs.Order;
using Service.Domain.Entities;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IOrderService : IBaseService<Order, OrderGetDto, OrderPostDto, OrderPutDto>
    {
        Task<PagedList<OrderGetDto>> GetFilteredAsync(int? idDestinyCenter, int? idOriginCenter, int? idProduct, bool? fictional, bool? isInbound, bool? isOutbound, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    }
}
