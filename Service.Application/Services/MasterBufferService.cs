using Service.Application.DTOs.MasterBuffer;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class MasterBufferService : BaseService<MasterBuffer, MasterBufferGetDto, MasterBufferPostDto, MasterBufferPutDto>, IMasterBufferService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public MasterBufferService(
            IMasterBufferRepository repository,
            IProductRepository productRepository,
            ICenterRepository centerRepository)
            : base(repository)
        {
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        protected override MasterBufferGetDto ToGetDTO(MasterBuffer entity)
        {
            return new MasterBufferGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                IdProductFather = entity.IdProductFather,
                IdCenterFather = entity.IdCenterFather,
                Sequency = entity.Sequency,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto(),
                ProductFather = entity.ProductFather?.ToGetDto(),
                CenterFather = entity.CenterFather?.ToGetDto()
            };
        }

        protected override MasterBuffer ToEntity(MasterBufferPostDto postDTO)
        {
            return new MasterBuffer
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                IdProductFather = postDTO.IdProductFather,
                IdCenterFather = postDTO.IdCenterFather,
                Sequency = postDTO.Sequency
            };
        }

        protected override void ApplyUpdate(MasterBuffer entity, MasterBufferPutDto putDTO)
        {
            entity.Sequency = putDTO.Sequency;
        }

        private async Task ValidateForeignKeysAsync(
            int idProduct, int idCenter, int idProductFather, int idCenterFather, CancellationToken cancellationToken)
        {
            if (!await _productRepository.Exists(idProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(idCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            if (!await _productRepository.Exists(idProductFather, cancellationToken))
                throw new BadRequestException("Father product not found.");

            if (!await _centerRepository.Exists(idCenterFather, cancellationToken))
                throw new BadRequestException("Father center not found.");
        }

        public override async Task<MasterBufferGetDto> AddAsync(MasterBufferPostDto postDTO, CancellationToken cancellationToken = default)
        {
            await ValidateForeignKeysAsync(postDTO.IdProduct, postDTO.IdCenter, postDTO.IdProductFather, postDTO.IdCenterFather, cancellationToken);

            return await base.AddAsync(postDTO, cancellationToken);
        }
    }
}
