using Service.Application.DTOs.Product;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class ProductService : BaseService<Product, ProductGetDto, ProductPostDto, ProductPutDto>, IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public ProductService(IProductRepository repository, ICenterRepository centerRepository) : base(repository)
        {
            _productRepository = repository;
            _centerRepository = centerRepository;
        }

        protected override ProductGetDto ToGetDTO(Product entity) => entity.ToGetDto();

        protected override Product ToEntity(ProductPostDto postDTO)
        {
            return new Product
            {
                Reference = postDTO.Reference,
                Description = postDTO.Description,
                AuxiliarMaterialCode = postDTO.AuxiliarMaterialCode,
                UnitOfMeasure = postDTO.UnitOfMeasure,
                Weight = postDTO.Weight,
                Volume = postDTO.Volume,
                Barcode = postDTO.Barcode,
                Category = postDTO.Category,
                Segment = postDTO.Segment,
                Value = postDTO.Value,
                Pallet = postDTO.Pallet,
                Line = postDTO.Line,
                Subline = postDTO.Subline,
                Brand = postDTO.Brand,
                WorkCenter = postDTO.WorkCenter
            };
        }

        protected override void ApplyUpdate(Product entity, ProductPutDto putDTO)
        {
            entity.Reference = putDTO.Reference;
            entity.Description = putDTO.Description;
            entity.AuxiliarMaterialCode = putDTO.AuxiliarMaterialCode;
            entity.UnitOfMeasure = putDTO.UnitOfMeasure;
            entity.Weight = putDTO.Weight;
            entity.Volume = putDTO.Volume;
            entity.Barcode = putDTO.Barcode;
            entity.Category = putDTO.Category;
            entity.Segment = putDTO.Segment;
            entity.Value = putDTO.Value;
            entity.Pallet = putDTO.Pallet;
            entity.Line = putDTO.Line;
            entity.Subline = putDTO.Subline;
            entity.Brand = putDTO.Brand;
            entity.WorkCenter = putDTO.WorkCenter;
        }

        public async Task<PagedList<ProductGetDto>> GetByCenterAsync(int idCenter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            if (!await _centerRepository.Exists(idCenter, cancellationToken))
                throw new NotFoundException("Center not found.");

            var paged = await _productRepository.GetByCenterAsync(idCenter, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<ProductGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }
    }
}
