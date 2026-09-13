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
        private readonly ICenterProductRepository _centerProductRepository;

        public BufferAdjustmentFactorService(
            IBufferAdjustmentFactorRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository,
            ICenterProductRepository centerProductRepository)
            : base(repository)
        {
            _productRepository = productRepository;
            _centerRepository = centerRepository;
            _centerProductRepository = centerProductRepository;
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
                BufferDdmrpRedSafeOld = entity.BufferDdmrpRedSafeOld,
                BufferDdmrpRedBaseOld = entity.BufferDdmrpRedBaseOld,
                BufferDdmrpYellowOld = entity.BufferDdmrpYellowOld,
                BufferDdmrpGreenOld = entity.BufferDdmrpGreenOld,
                AlreadyReverted = entity.AlreadyReverted,
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
                BufferDdmrpRedSafeOld = null,
                BufferDdmrpRedBaseOld = null,
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

            var entity = ToEntity(postDTO);

            var centerProduct = await _centerProductRepository.GetByProductAndCenterAsync(postDTO.IdProduct, postDTO.IdCenter, cancellationToken);
            if (centerProduct != null)
            {
                entity.BufferTypeOld = centerProduct.BufferType;
                entity.BufferDdmrpRedSafeOld = centerProduct.RedZoneSafe;
                entity.BufferDdmrpRedBaseOld = centerProduct.RedZoneBase;
                entity.BufferDdmrpYellowOld = centerProduct.YellowZone;
                entity.BufferDdmrpGreenOld = centerProduct.GreenZone;
            }

            var created = await _repository.AddAsync(entity, cancellationToken);
            return ToGetDTO(created);
        }

        public async Task<BufferAdjustmentFactorGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");

            if (!isActive && IsActiveAndInPeriod(entity) && !entity.AlreadyReverted)
            {
                await RevertCenterProductAsync(entity, cancellationToken);
                entity.AlreadyReverted = true;
            }
            else if (isActive)
            {
                entity.AlreadyReverted = false;
            }

            entity.IsActive = isActive;
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }

        public override async Task<BufferAdjustmentFactorGetDto> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Not found");

            if (IsActiveAndInPeriod(entity) && !entity.AlreadyReverted)
            {
                await RevertCenterProductAsync(entity, cancellationToken);
                entity.AlreadyReverted = true;
            }

            var deleted = await _repository.DeleteAsync(id, cancellationToken);
            return ToGetDTO(deleted);
        }

        private static bool IsActiveAndInPeriod(BufferAdjustmentFactor entity)
        {
            var now = DateTime.Now;
            return entity.IsActive && entity.EffectiveFrom <= now && entity.EffectiveTo >= now;
        }

        private async Task RevertCenterProductAsync(BufferAdjustmentFactor baf, CancellationToken cancellationToken)
        {
            var centerProduct = await _centerProductRepository.GetByProductAndCenterAsync(baf.IdProduct, baf.IdCenter, cancellationToken);
            if (centerProduct == null)
                return;

            if (baf.BufferTypeOld.HasValue)
                centerProduct.BufferType = baf.BufferTypeOld.Value;
            if (baf.BufferDdmrpRedSafeOld.HasValue)
                centerProduct.RedZoneSafe = baf.BufferDdmrpRedSafeOld;
            if (baf.BufferDdmrpRedBaseOld.HasValue)
                centerProduct.RedZoneBase = baf.BufferDdmrpRedBaseOld;
            if (baf.BufferDdmrpYellowOld.HasValue)
                centerProduct.YellowZone = baf.BufferDdmrpYellowOld;
            if (baf.BufferDdmrpGreenOld.HasValue)
                centerProduct.GreenZone = baf.BufferDdmrpGreenOld;

            await _centerProductRepository.UpdateAsync(centerProduct, cancellationToken);
        }
    }
}
