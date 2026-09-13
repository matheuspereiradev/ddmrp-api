using Service.Application.DTOs.Order;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IOrderService : IBaseService<Order, OrderGetDto, OrderPostDto, OrderPutDto>
    {
    }
}
