using Service.Application.DTOs.AllocationGroup;
using Service.Application.DTOs.BufferProfile;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.Partner;
using Service.Application.DTOs.Product;
using Service.Application.DTOs.Reason;
using Service.Application.DTOs.Role;
using Service.Application.DTOs.Tag;
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

        public static PartnerGetDto ToGetDto(this Partner partner) => new()
        {
            Id = partner.Id,
            Code = partner.Code,
            Description = partner.Description
        };

        public static TagGetDto ToGetDto(this Tag tag) => new()
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description
        };

        public static ReasonGetDto ToGetDto(this Reason reason) => new()
        {
            Id = reason.Id,
            Name = reason.Name,
            Description = reason.Description,
            IsFromSystem = reason.IsFromSystem
        };

        public static AllocationGroupGetDto ToGetDto(this AllocationGroup allocationGroup) => new()
        {
            Id = allocationGroup.Id,
            Name = allocationGroup.Name
        };

        public static BufferProfileGetDto ToGetDto(this BufferProfile bufferProfile) => new()
        {
            Id = bufferProfile.Id,
            ProfileName = bufferProfile.ProfileName,
            SupplyType = bufferProfile.SupplyType,
            LeadTimeCategory = bufferProfile.LeadTimeCategory,
            VariabilityCategory = bufferProfile.VariabilityCategory,
            LeadTimeFactor = bufferProfile.LeadTimeFactor,
            VariabilityFactor = bufferProfile.VariabilityFactor,
            AduCalculationDays = bufferProfile.AduCalculationDays,
            AduFutureDays = bufferProfile.AduFutureDays,
            Frequency = bufferProfile.Frequency,
            UseAdUxDlTxFactorDlt = bufferProfile.UseAdUxDlTxFactorDlt,
            UseMoq = bufferProfile.UseMoq,
            UseAdUxFrequency = bufferProfile.UseAdUxFrequency,
            SpikeHorizonType = bufferProfile.SpikeHorizonType,
            SpikeHorizonValue = bufferProfile.SpikeHorizonValue,
            SpikeHorizonLTDays = bufferProfile.SpikeHorizonLTDays,
            SpikeThresholdType = bufferProfile.SpikeThresholdType,
            SpikeThresholdAdu = bufferProfile.SpikeThresholdAdu,
            SpikeThresholdPercentageRedZone = bufferProfile.SpikeThresholdPercentageRedZone,
            IsActive = bufferProfile.IsActive,
            IsMakeToOrder = bufferProfile.IsMakeToOrder
        };
    }
}
