using Service.Application.DTOs.Order;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Domain.Utils;

namespace Service.Application.Services
{
    public class OrderService : BaseService<Order, OrderGetDto, OrderPostDto, OrderPutDto>, IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPartnerRepository _partnerRepository;
        private readonly ICenterRepository _centerRepository;

        public OrderService(
            IOrderRepository repository,
            IProductRepository productRepository,
            IPartnerRepository partnerRepository,
            ICenterRepository centerRepository)
            : base(repository)
        {
            _orderRepository = repository;
            _productRepository = productRepository;
            _partnerRepository = partnerRepository;
            _centerRepository = centerRepository;
        }

        public async Task<PagedList<OrderGetDto>> GetFilteredAsync(int? idDestinyCenter, int? idOriginCenter, bool? fictional, bool? isInbound, bool? isOutbound, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _orderRepository.GetFilteredAsync(idDestinyCenter, idOriginCenter, fictional, isInbound, isOutbound, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<OrderGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        protected override OrderGetDto ToGetDTO(Order entity)
        {
            return new OrderGetDto
            {
                Id = entity.Id,
                OrderNumber = entity.OrderNumber,
                IdPartner = entity.IdPartner,
                IdDestinyCenter = entity.IdDestinyCenter,
                IdOriginCenter = entity.IdOriginCenter,
                IdProduct = entity.IdProduct,
                Quantity = entity.Quantity,
                DeliveredQuantity = entity.DeliveredQuantity,
                PendingQuantity = entity.PendingQuantity,
                MeasurementUnit = entity.MeasurementUnit,
                Position = entity.Position,
                CreationDate = entity.CreationDate,
                DeliveryDate = entity.DeliveryDate,
                OrderLeadtime = entity.OrderLeadtime,
                DaysToReceive = UtilsDdmrp.CalculateDaysToReceive(entity.DeliveryDate),
                DaysLate = UtilsDdmrp.CalculateDaysLate(entity.DeliveryDate),
                Notes = entity.Notes,
                Type = entity.Type,
                IsInbound = entity.IsInbound,
                IsOutbound = entity.IsOutbound,
                IsFictional = entity.IsFictional,
                Partner = entity.Partner?.ToGetDto(),
                DestinyCenter = entity.DestinyCenter?.ToGetDto(),
                OriginCenter = entity.OriginCenter?.ToGetDto(),
                Product = entity.Product?.ToGetDto()
            };
        }

        protected override Order ToEntity(OrderPostDto postDTO)
        {
            return new Order
            {
                OrderNumber = postDTO.OrderNumber,
                IdPartner = postDTO.IdPartner,
                IdDestinyCenter = postDTO.IdDestinyCenter,
                IdOriginCenter = postDTO.IdOriginCenter,
                IdProduct = postDTO.IdProduct,
                Quantity = postDTO.Quantity,
                DeliveredQuantity = postDTO.DeliveredQuantity,
                MeasurementUnit = postDTO.MeasurementUnit,
                Position = postDTO.Position,
                CreationDate = postDTO.CreationDate,
                DeliveryDate = postDTO.DeliveryDate,
                Notes = postDTO.Notes,
                Type = postDTO.Type,
                IsInbound = postDTO.IsInbound,
                IsOutbound = postDTO.IsOutbound,
                IsFictional = postDTO.IsFictional
            };
        }

        protected override void ApplyUpdate(Order entity, OrderPutDto putDTO)
        {
            entity.IdPartner = putDTO.IdPartner;
            entity.Quantity = putDTO.Quantity;
            entity.DeliveredQuantity = putDTO.DeliveredQuantity;
            entity.MeasurementUnit = putDTO.MeasurementUnit;
            entity.Position = putDTO.Position;
            entity.CreationDate = putDTO.CreationDate;
            entity.DeliveryDate = putDTO.DeliveryDate;
            entity.Notes = putDTO.Notes;
        }

        private async Task ValidateOptionalForeignKeysAsync(int? idPartner, int? idDestinyCenter, int? idOriginCenter, CancellationToken cancellationToken)
        {
            if (idPartner.HasValue && !await _partnerRepository.Exists(idPartner.Value, cancellationToken))
                throw new BadRequestException("Partner not found.");

            if (idDestinyCenter.HasValue && !await _centerRepository.Exists(idDestinyCenter.Value, cancellationToken))
                throw new BadRequestException("Destiny center not found.");

            if (idOriginCenter.HasValue && !await _centerRepository.Exists(idOriginCenter.Value, cancellationToken))
                throw new BadRequestException("Origin center not found.");
        }

        public override async Task<OrderGetDto> AddAsync(OrderPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            await ValidateOptionalForeignKeysAsync(postDTO.IdPartner, postDTO.IdDestinyCenter, postDTO.IdOriginCenter, cancellationToken);

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public override async Task<OrderGetDto> UpdateAsync(int id, OrderPutDto putDTO, CancellationToken cancellationToken = default)
        {
            if (putDTO.IdPartner.HasValue && !await _partnerRepository.Exists(putDTO.IdPartner.Value, cancellationToken))
                throw new BadRequestException("Partner not found.");

            return await base.UpdateAsync(id, putDTO, cancellationToken);
        }
    }
}
