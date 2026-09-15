using Service.API.Extensions;
using Service.API.Models;
using Service.Application.DTOs.Order;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult> CreateOrder(OrderPostDto orderPostDto, CancellationToken cancellationToken)
        {
            var order = await _orderService.AddAsync(orderPostDto, cancellationToken);
            return Ok(order);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult> GetAllOrders(
            [FromQuery] int? idDestinyCenter,
            [FromQuery] int? idOriginCenter,
            [FromQuery] bool? fictional,
            [FromQuery] bool? isInbound,
            [FromQuery] bool? isOutbound,
            [FromQuery] PaginationParams paginationParams,
            CancellationToken cancellationToken)
        {
            var orders = await _orderService.GetFilteredAsync(idDestinyCenter, idOriginCenter, fictional, isInbound, isOutbound, paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, orders.TotalCount, orders.TotalPages));

            return Ok(orders);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> UpdateOrder(int id, OrderPutDto orderPutDto, CancellationToken cancellationToken)
        {
            var order = await _orderService.UpdateAsync(id, orderPutDto, cancellationToken);
            return Ok(order);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteOrder(int id, CancellationToken cancellationToken)
        {
            var order = await _orderService.DeleteAsync(id, cancellationToken);
            return Ok(order);
        }
    }
}
