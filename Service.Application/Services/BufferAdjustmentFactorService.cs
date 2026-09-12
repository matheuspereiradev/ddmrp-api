using Service.Application.DTOs.BufferAdjustmentFactor;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class BufferAdjustmentFactorService : BaseService<BufferAdjustmentFactor, BufferAdjustmentFactorGetDto, BufferAdjustmentFactorPostDto, BufferAdjustmentFactorPutDto>, IBufferAdjustmentFactorService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public BufferAdjustmentFactorService(
            IBufferAdjustmentFactorRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository)
            : base(repository)
        {
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        protected override BufferAdjustmentFactorGetDto ToGetDTO(BufferAdjustmentFactor entity)
        {
            return new BufferAdjustmentFactorGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                BufferType = entity.BufferType,
                BufferDdmrpRed = entity.BufferDdmrpRed,
                BufferDdmrpYellow = entity.BufferDdmrpYellow,
                BufferDdmrpGreen = entity.BufferDdmrpGreen,
                Obs = entity.Obs,
                IsActive = entity.IsActive,
                BufferTypeOld = entity.BufferTypeOld,
                BufferDdmrpRedOld = entity.BufferDdmrpRedOld,
                BufferDdmrpYellowOld = entity.BufferDdmrpYellowOld,
                BufferDdmrpGreenOld = entity.BufferDdmrpGreenOld,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto()
            };
        }

        protected override BufferAdjustmentFactor ToEntity(BufferAdjustmentFactorPostDto postDTO)
        {
            return new BufferAdjustmentFactor
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                EffectiveFrom = postDTO.EffectiveFrom,
                EffectiveTo = postDTO.EffectiveTo,
                BufferType = postDTO.BufferType,
                BufferDdmrpRed = postDTO.BufferDdmrpRed,
                BufferDdmrpYellow = postDTO.BufferDdmrpYellow,
                BufferDdmrpGreen = postDTO.BufferDdmrpGreen,
                Obs = postDTO.Obs,
                IsActive = postDTO.IsActive,
                BufferTypeOld = null,
                BufferDdmrpRedOld = null,
                BufferDdmrpYellowOld = null,
                BufferDdmrpGreenOld = null
            };
        }

        protected override void ApplyUpdate(BufferAdjustmentFactor entity, BufferAdjustmentFactorPutDto putDTO)
        {
            entity.EffectiveFrom = putDTO.EffectiveFrom;
            entity.EffectiveTo = putDTO.EffectiveTo;
            entity.BufferType = putDTO.BufferType;
            entity.BufferDdmrpRed = putDTO.BufferDdmrpRed;
            entity.BufferDdmrpYellow = putDTO.BufferDdmrpYellow;
            entity.BufferDdmrpGreen = putDTO.BufferDdmrpGreen;
            entity.Obs = putDTO.Obs;
            entity.IsActive = putDTO.IsActive;
        }

        public override async Task<BufferAdjustmentFactorGetDto> AddAsync(BufferAdjustmentFactorPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public async Task<BufferAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
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
