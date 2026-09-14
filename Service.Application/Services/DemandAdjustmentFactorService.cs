using Service.Application.DTOs.DemandAdjustmentFactor;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class DemandAdjustmentFactorService : BaseService<DemandAdjustmentFactor, DemandAdjustmentFactorGetDto, DemandAdjustmentFactorPostDto, DemandAdjustmentFactorPutDto>, IDemandAdjustmentFactorService
    {
        private readonly IDemandAdjustmentFactorRepository _demandAdjustmentFactorRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public DemandAdjustmentFactorService(
            IDemandAdjustmentFactorRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository)
            : base(repository)
        {
            _demandAdjustmentFactorRepository = repository;
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        protected override DemandAdjustmentFactorGetDto ToGetDTO(DemandAdjustmentFactor entity)
        {
            return new DemandAdjustmentFactorGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                IsActive = entity.IsActive,
                Obs = entity.Obs,
                AdjustmentType = entity.AdjustmentType,
                AdjustmentValue = entity.AdjustmentValue,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto()
            };
        }

        protected override DemandAdjustmentFactor ToEntity(DemandAdjustmentFactorPostDto postDTO)
        {
            return new DemandAdjustmentFactor
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                EffectiveFrom = postDTO.EffectiveFrom,
                EffectiveTo = postDTO.EffectiveTo,
                IsActive = postDTO.IsActive,
                Obs = postDTO.Obs,
                AdjustmentType = postDTO.AdjustmentType,
                AdjustmentValue = postDTO.AdjustmentValue
            };
        }

        protected override void ApplyUpdate(DemandAdjustmentFactor entity, DemandAdjustmentFactorPutDto putDTO)
        {
            entity.IsActive = putDTO.IsActive;
            entity.Obs = putDTO.Obs;
            entity.AdjustmentType = putDTO.AdjustmentType;
            entity.AdjustmentValue = putDTO.AdjustmentValue;
        }

        public override async Task<DemandAdjustmentFactorGetDto> AddAsync(DemandAdjustmentFactorPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            if (await _demandAdjustmentFactorRepository.ExistsOverlappingAsync(postDTO.IdProduct, postDTO.IdCenter, postDTO.EffectiveFrom, postDTO.EffectiveTo, cancellationToken))
                throw new BadRequestException("There is already an active demand adjustment factor for this product/center in the given period.");

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public async Task<DemandAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
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
