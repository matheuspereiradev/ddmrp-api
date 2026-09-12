using Service.Application.DTOs.Product;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class ProductService : BaseService<Product, ProductGetDto, ProductPostDto, ProductPutDto>, IProductService
    {
        public ProductService(IProductRepository repository) : base(repository)
        {
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
                ABC = postDTO.ABC,
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
            entity.ABC = putDTO.ABC;
            entity.Brand = putDTO.Brand;
            entity.WorkCenter = putDTO.WorkCenter;
        }
    }
}
