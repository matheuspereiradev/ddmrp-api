using Service.Application.DTOs.History;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class HistoryService : BaseService<History, HistoryGetDto, HistoryPostDto, HistoryPutDto>, IHistoryService
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public HistoryService(IHistoryRepository repository, IProductRepository productRepository, ICenterRepository centerRepository)
            : base(repository)
        {
            _historyRepository = repository;
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        protected override HistoryGetDto ToGetDTO(History entity)
        {
            return new HistoryGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                Consumption = entity.Consumption,
                Date = entity.Date,
                DiscardStatus = entity.DiscardStatus,
                Stock = entity.Stock,
                ReservedStock = entity.ReservedStock,
                AvailableStock = entity.AvailableStock,
                QualifiedDemand = entity.QualifiedDemand,
                OpenInbounds = entity.OpenInbounds,
                OpenOutbound = entity.OpenOutbound,
                Adu = entity.Adu,
                RedSafeZone = entity.RedSafeZone,
                RedBaseZone = entity.RedBaseZone,
                YellowZone = entity.YellowZone,
                GreenZone = entity.GreenZone,
                PackQuantity = entity.PackQuantity,
                Moq = entity.Moq,
                LeadTime = entity.LeadTime,
                Frequency = entity.Frequency,
                IdTag = entity.IdTag,
                IdReason = entity.IdReason,
                IdBufferProfile = entity.IdBufferProfile,
                BufferProfile = entity.BufferProfile?.ToGetDto(),
                StandardDeviation = entity.StandardDeviation,
                Cv = entity.Cv,
                FutureAduDays = entity.FutureAduDays,
                HistoryAduDays = entity.HistoryAduDays,
                Adi = entity.Adi,
                ZafRedZone = entity.ZafRedZone,
                ZafYellowZone = entity.ZafYellowZone,
                ZafGreenZone = entity.ZafGreenZone,
                StockDays = entity.StockDays,
                StockTotal = entity.StockTotal,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto()
            };
        }

        protected override History ToEntity(HistoryPostDto postDTO)
        {
            return new History
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                Consumption = postDTO.Consumption,
                Date = postDTO.Date,
                DiscardStatus = DiscardStatus.NotReviewed
            };
        }

        protected override void ApplyUpdate(History entity, HistoryPutDto putDTO)
        {
            entity.Consumption = putDTO.Consumption;
            entity.DiscardStatus = putDTO.DiscardStatus;
        }

        public override async Task<HistoryGetDto> AddAsync(HistoryPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public async Task<PagedList<HistoryGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _historyRepository.GetFilteredAsync(idProduct, idCenter, dateStart, dateEnd, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<HistoryGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public async Task<HistoryGetDto> SetDiscardStatusAsync(int id, DiscardStatus discardStatus, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");

            entity.DiscardStatus = discardStatus;
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }
    }
}
