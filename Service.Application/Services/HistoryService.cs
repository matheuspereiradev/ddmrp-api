using Service.Application.DTOs.History;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class HistoryService : BaseService<History, HistoryGetDto, HistoryPostDto, HistoryPutDto>, IHistoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public HistoryService(IHistoryRepository repository, IProductRepository productRepository, ICenterRepository centerRepository)
            : base(repository)
        {
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
                Quantity = entity.Quantity,
                Date = entity.Date,
                DiscardStatus = entity.DiscardStatus,
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
                Quantity = postDTO.Quantity,
                Date = postDTO.Date,
                DiscardStatus = DiscardStatus.NotReviewed
            };
        }

        protected override void ApplyUpdate(History entity, HistoryPutDto putDTO)
        {
            entity.Quantity = putDTO.Quantity;
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
    }
}
