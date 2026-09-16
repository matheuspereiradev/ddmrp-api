using Service.Application.DTOs.CenterProduct;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class CenterProductService : BaseService<CenterProduct, CenterProductGetDto, CenterProductPostDto, CenterProductPutDto>, ICenterProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly IPartnerRepository _partnerRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IReasonRepository _reasonRepository;
        private readonly IAllocationGroupRepository _allocationGroupRepository;
        private readonly IBufferProfileRepository _bufferProfileRepository;

        public CenterProductService(
            ICenterProductRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository,
            IPartnerRepository partnerRepository,
            ITagRepository tagRepository,
            IReasonRepository reasonRepository,
            IAllocationGroupRepository allocationGroupRepository,
            IBufferProfileRepository bufferProfileRepository)
            : base(repository)
        {
            _productRepository = productRepository;
            _centerRepository = centerRepository;
            _partnerRepository = partnerRepository;
            _tagRepository = tagRepository;
            _reasonRepository = reasonRepository;
            _allocationGroupRepository = allocationGroupRepository;
            _bufferProfileRepository = bufferProfileRepository;
        }

        protected override CenterProductGetDto ToGetDTO(CenterProduct entity) => entity.ToGetDto();

        protected override CenterProduct ToEntity(CenterProductPostDto postDTO)
        {
            return new CenterProduct
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                IdOriginCenter = postDTO.IdOriginCenter,
                PackQuantity = postDTO.PackQuantity,
                Moq = postDTO.Moq,
                LeadTime = postDTO.LeadTime,
                Frequency = postDTO.Frequency,
                Class = postDTO.Class,
                Classification = postDTO.Classification,
                Segment = postDTO.Segment,
                Stock = postDTO.Stock,
                IdProvider = postDTO.IdProvider,
                IdTag = postDTO.IdTag,
                IdReason = postDTO.IdReason,
                IdAllocationGroup = postDTO.IdAllocationGroup,
                IdBufferProfile = postDTO.IdBufferProfile,
                FutureAduDays = postDTO.FutureAduDays,
                HistoryAduDays = postDTO.HistoryAduDays,
                UseSuggestedLTFactor = postDTO.UseSuggestedLTFactor,
                UseSuggestedVariabilityFactor = postDTO.UseSuggestedVariabilityFactor,
                FixedBufferProfile = postDTO.FixedBufferProfile,
                UseDafOnGreenZone = postDTO.UseDafOnGreenZone,
                CustomLeadTimeFactor = postDTO.CustomLeadTimeFactor,
                CustomVariabilityFactor = postDTO.CustomVariabilityFactor,
                GreenZoneParametrizationUseMoq = postDTO.GreenZoneParametrizationUseMoq,
                GreenZoneParametrizationUseAduXFrequency = postDTO.GreenZoneParametrizationUseAduXFrequency,
                GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = postDTO.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime,
                BufferType = postDTO.BufferType,
                SpikeHorizonType = postDTO.SpikeHorizonType,
                SpikeHorizonValue = postDTO.SpikeHorizonValue,
                SpikeHorizonLTDays = postDTO.SpikeHorizonLTDays,
                SpikeThresholdType = postDTO.SpikeThresholdType,
                SpikeThresholdAdu = postDTO.SpikeThresholdAdu,
                SpikeThresholdPercentageRedZone = postDTO.SpikeThresholdPercentageRedZone
            };
        }

        protected override void ApplyUpdate(CenterProduct entity, CenterProductPutDto putDTO)
        {
            entity.IdOriginCenter = putDTO.IdOriginCenter;
            entity.PackQuantity = putDTO.PackQuantity;
            entity.Moq = putDTO.Moq;
            entity.LeadTime = putDTO.LeadTime;
            entity.Frequency = putDTO.Frequency;
            entity.Class = putDTO.Class;
            entity.Classification = putDTO.Classification;
            entity.Segment = putDTO.Segment;
            entity.Stock = putDTO.Stock;
            entity.IdProvider = putDTO.IdProvider;
            entity.IdTag = putDTO.IdTag;
            entity.IdReason = putDTO.IdReason;
            entity.IdAllocationGroup = putDTO.IdAllocationGroup;
            entity.IdBufferProfile = putDTO.IdBufferProfile;
            entity.FutureAduDays = putDTO.FutureAduDays;
            entity.HistoryAduDays = putDTO.HistoryAduDays;
            entity.UseSuggestedLTFactor = putDTO.UseSuggestedLTFactor;
            entity.UseSuggestedVariabilityFactor = putDTO.UseSuggestedVariabilityFactor;
            entity.FixedBufferProfile = putDTO.FixedBufferProfile;
            entity.UseDafOnGreenZone = putDTO.UseDafOnGreenZone;
            entity.CustomLeadTimeFactor = putDTO.CustomLeadTimeFactor;
            entity.CustomVariabilityFactor = putDTO.CustomVariabilityFactor;
            entity.GreenZoneParametrizationUseMoq = putDTO.GreenZoneParametrizationUseMoq;
            entity.GreenZoneParametrizationUseAduXFrequency = putDTO.GreenZoneParametrizationUseAduXFrequency;
            entity.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = putDTO.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime;
            entity.BufferType = putDTO.BufferType;

            if (putDTO.BufferType == BufferType.ManualFixed)
            {
                entity.RedZoneBase = putDTO.RedZoneBase;
                entity.RedZoneSafe = putDTO.RedZoneSafe;
                entity.YellowZone = putDTO.YellowZone;
                entity.GreenZone = putDTO.GreenZone;
            }
            else if (putDTO.RedZoneBase.HasValue || putDTO.RedZoneSafe.HasValue || putDTO.YellowZone.HasValue || putDTO.GreenZone.HasValue)
            {
                throw new BadRequestException("RedZoneBase, RedZoneSafe, YellowZone and GreenZone can only be edited when BufferType is ManualFixed.");
            }

            entity.SpikeHorizonType = putDTO.SpikeHorizonType;
            entity.SpikeHorizonValue = putDTO.SpikeHorizonValue;
            entity.SpikeHorizonLTDays = putDTO.SpikeHorizonLTDays;
            entity.SpikeThresholdType = putDTO.SpikeThresholdType;
            entity.SpikeThresholdAdu = putDTO.SpikeThresholdAdu;
            entity.SpikeThresholdPercentageRedZone = putDTO.SpikeThresholdPercentageRedZone;
        }

        private async Task ValidateOptionalForeignKeysAsync(
            int? idOriginCenter,
            int? idProvider,
            int? idTag,
            int? idReason,
            int? idAllocationGroup,
            int? idBufferProfile,
            CancellationToken cancellationToken)
        {
            if (idOriginCenter.HasValue && !await _centerRepository.Exists(idOriginCenter.Value, cancellationToken))
                throw new BadRequestException("Origin center not found.");

            if (idProvider.HasValue && !await _partnerRepository.Exists(idProvider.Value, cancellationToken))
                throw new BadRequestException("Provider not found.");

            if (idTag.HasValue && !await _tagRepository.Exists(idTag.Value, cancellationToken))
                throw new BadRequestException("Tag not found.");

            if (idReason.HasValue && !await _reasonRepository.Exists(idReason.Value, cancellationToken))
                throw new BadRequestException("Reason not found.");

            if (idAllocationGroup.HasValue && !await _allocationGroupRepository.Exists(idAllocationGroup.Value, cancellationToken))
                throw new BadRequestException("Allocation group not found.");

            if (idBufferProfile.HasValue && !await _bufferProfileRepository.Exists(idBufferProfile.Value, cancellationToken))
                throw new BadRequestException("Buffer profile not found.");
        }

        public override async Task<CenterProductGetDto> AddAsync(CenterProductPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            await ValidateOptionalForeignKeysAsync(
                postDTO.IdOriginCenter, postDTO.IdProvider, postDTO.IdTag, postDTO.IdReason, postDTO.IdAllocationGroup, postDTO.IdBufferProfile,
                cancellationToken);

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public override async Task<CenterProductGetDto> UpdateAsync(int id, CenterProductPutDto putDTO, CancellationToken cancellationToken = default)
        {
            await ValidateOptionalForeignKeysAsync(
                putDTO.IdOriginCenter, putDTO.IdProvider, putDTO.IdTag, putDTO.IdReason, putDTO.IdAllocationGroup, putDTO.IdBufferProfile,
                cancellationToken);

            return await base.UpdateAsync(id, putDTO, cancellationToken);
        }

        public async Task<CenterProductGetDto> SetAllocationGroupAsync(int id, int? idAllocationGroup, CancellationToken cancellationToken = default)
        {
            if (idAllocationGroup.HasValue && !await _allocationGroupRepository.Exists(idAllocationGroup.Value, cancellationToken))
                throw new BadRequestException("Allocation group not found.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Not found");

            entity.IdAllocationGroup = idAllocationGroup;
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }

        public async Task<CenterProductGetDto> SetTagAsync(int id, int? idTag, CancellationToken cancellationToken = default)
        {
            if (idTag.HasValue && !await _tagRepository.Exists(idTag.Value, cancellationToken))
                throw new BadRequestException("Tag not found.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Not found");

            entity.IdTag = idTag;
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }

        public async Task<CenterProductGetDto> SetReasonAsync(int id, int? idReason, CancellationToken cancellationToken = default)
        {
            if (idReason.HasValue && !await _reasonRepository.Exists(idReason.Value, cancellationToken))
                throw new BadRequestException("Reason not found.");

            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Not found");

            entity.IdReason = idReason;
            var updated = await _repository.UpdateAsync(entity, cancellationToken);
            return ToGetDTO(updated);
        }
    }
}
