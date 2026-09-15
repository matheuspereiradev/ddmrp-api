using Service.Application.DTOs.ZoneAdjustmentFactor;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class ZoneAdjustmentFactorService : BaseService<ZoneAdjustmentFactor, ZoneAdjustmentFactorGetDto, ZoneAdjustmentFactorPostDto, ZoneAdjustmentFactorPutDto>, IZoneAdjustmentFactorService
    {
        private readonly IZoneAdjustmentFactorRepository _zoneAdjustmentFactorRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public ZoneAdjustmentFactorService(
            IZoneAdjustmentFactorRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository)
            : base(repository)
        {
            _zoneAdjustmentFactorRepository = repository;
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        protected override ZoneAdjustmentFactorGetDto ToGetDTO(ZoneAdjustmentFactor entity)
        {
            return new ZoneAdjustmentFactorGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                TargetZone = entity.TargetZone,
                AdjustmentType = entity.AdjustmentType,
                AdjustmentValue = entity.AdjustmentValue,
                Obs = entity.Obs,
                IsActive = entity.IsActive,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto()
            };
        }

        protected override ZoneAdjustmentFactor ToEntity(ZoneAdjustmentFactorPostDto postDTO)
        {
            return new ZoneAdjustmentFactor
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                TargetZone = postDTO.TargetZone,
                AdjustmentType = postDTO.AdjustmentType,
                AdjustmentValue = postDTO.AdjustmentValue,
                Obs = postDTO.Obs,
                IsActive = postDTO.IsActive,
                EffectiveFrom = postDTO.EffectiveFrom,
                EffectiveTo = postDTO.EffectiveTo
            };
        }

        protected override void ApplyUpdate(ZoneAdjustmentFactor entity, ZoneAdjustmentFactorPutDto putDTO)
        {
            entity.TargetZone = putDTO.TargetZone;
            entity.AdjustmentType = putDTO.AdjustmentType;
            entity.AdjustmentValue = putDTO.AdjustmentValue;
            entity.Obs = putDTO.Obs;
            entity.IsActive = putDTO.IsActive;
        }

        public override async Task<ZoneAdjustmentFactorGetDto> AddAsync(ZoneAdjustmentFactorPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            if (await _zoneAdjustmentFactorRepository.ExistsOverlappingAsync(postDTO.IdProduct, postDTO.IdCenter, postDTO.TargetZone, postDTO.EffectiveFrom, postDTO.EffectiveTo, cancellationToken))
                throw new BadRequestException("There is already an active zone adjustment factor for this product/center/zone in the given period.");

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public async Task<PagedList<ZoneAdjustmentFactorGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _zoneAdjustmentFactorRepository.GetFilteredAsync(idProduct, idCenter, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<ZoneAdjustmentFactorGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public async Task<ZoneAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");

            entity.IsActive = isActive;
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }
    }
}
