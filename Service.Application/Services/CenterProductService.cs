using Service.Application.DTOs.CenterProduct;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
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

        public CenterProductService(
            ICenterProductRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository,
            IPartnerRepository partnerRepository,
            ITagRepository tagRepository,
            IReasonRepository reasonRepository,
            IAllocationGroupRepository allocationGroupRepository)
            : base(repository)
        {
            _productRepository = productRepository;
            _centerRepository = centerRepository;
            _partnerRepository = partnerRepository;
            _tagRepository = tagRepository;
            _reasonRepository = reasonRepository;
            _allocationGroupRepository = allocationGroupRepository;
        }

        protected override CenterProductGetDto ToGetDTO(CenterProduct entity)
        {
            return new CenterProductGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                IdOriginCenter = entity.IdOriginCenter,
                PackQuantity = entity.PackQuantity,
                Moq = entity.Moq,
                LeadTime = entity.LeadTime,
                Frequency = entity.Frequency,
                Class = entity.Class,
                Classification = entity.Classification,
                Segment = entity.Segment,
                Stock = entity.Stock,
                IdProvider = entity.IdProvider,
                IdTag = entity.IdTag,
                IdReason = entity.IdReason,
                IdAllocationGroup = entity.IdAllocationGroup,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto(),
                OriginCenter = entity.OriginCenter?.ToGetDto(),
                Provider = entity.Provider?.ToGetDto(),
                Tag = entity.Tag?.ToGetDto(),
                Reason = entity.Reason?.ToGetDto(),
                AllocationGroup = entity.AllocationGroup?.ToGetDto()
            };
        }

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
                IdAllocationGroup = postDTO.IdAllocationGroup
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
        }

        private async Task ValidateOptionalForeignKeysAsync(
            int? idOriginCenter,
            int? idProvider,
            int? idTag,
            int? idReason,
            int? idAllocationGroup,
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
        }

        public override async Task<CenterProductGetDto> AddAsync(CenterProductPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            await ValidateOptionalForeignKeysAsync(
                postDTO.IdOriginCenter, postDTO.IdProvider, postDTO.IdTag, postDTO.IdReason, postDTO.IdAllocationGroup,
                cancellationToken);

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public override async Task<CenterProductGetDto> UpdateAsync(int id, CenterProductPutDto putDTO, CancellationToken cancellationToken = default)
        {
            await ValidateOptionalForeignKeysAsync(
                putDTO.IdOriginCenter, putDTO.IdProvider, putDTO.IdTag, putDTO.IdReason, putDTO.IdAllocationGroup,
                cancellationToken);

            return await base.UpdateAsync(id, putDTO, cancellationToken);
        }
    }
}
