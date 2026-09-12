using Service.Application.DTOs.Center;
using Service.Application.DTOs.Product;
using Service.Application.DTOs.Role;
using Service.Domain.Entities;

namespace Service.Application.Mappers
{
    public static class EntityMappingExtensions
    {
        public static RoleGetDto ToGetDto(this Role role) => new()
        {
            Id = role.Id,
            Name = role.Name
        };

        public static CenterGetDto ToGetDto(this Center center) => new()
        {
            Id = center.Id,
            Code = center.Code,
            Description = center.Description,
            City = center.City,
            Zone = center.Zone
        };

        public static ProductGetDto ToGetDto(this Product product) => new()
        {
            Id = product.Id,
            Reference = product.Reference,
            Description = product.Description,
            AuxiliarMaterialCode = product.AuxiliarMaterialCode,
            UnitOfMeasure = product.UnitOfMeasure,
            Weight = product.Weight,
            Volume = product.Volume,
            Barcode = product.Barcode,
            Category = product.Category,
            Segment = product.Segment,
            Value = product.Value,
            Pallet = product.Pallet,
            Line = product.Line,
            Subline = product.Subline,
            ABC = product.ABC,
            Brand = product.Brand,
            WorkCenter = product.WorkCenter
        };
    }
}
