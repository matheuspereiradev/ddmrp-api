using Service.Application.DTOs.AllocationGroup;
using Service.Application.DTOs.BufferProfile;
using Service.Application.DTOs.Center;
using Service.Application.DTOs.CenterProduct;
using Service.Application.DTOs.Partner;
using Service.Application.DTOs.Product;
using Service.Application.DTOs.Reason;
using Service.Application.DTOs.Role;
using Service.Application.DTOs.Tag;
using Service.Application.DTOs.User;
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

        public static UserGetDto ToGetDto(this User user) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            IdRole = user.IdRole,
            Role = user.Role?.ToGetDto()
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
            GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = bufferProfile.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime,
            GreenZoneParametrizationUseMoq = bufferProfile.GreenZoneParametrizationUseMoq,
            GreenZoneParametrizationUseAduXFrequency = bufferProfile.GreenZoneParametrizationUseAduXFrequency,
            SpikeHorizonType = bufferProfile.SpikeHorizonType,
            SpikeHorizonValue = bufferProfile.SpikeHorizonValue,
            SpikeHorizonLTDays = bufferProfile.SpikeHorizonLTDays,
            SpikeThresholdType = bufferProfile.SpikeThresholdType,
            SpikeThresholdAdu = bufferProfile.SpikeThresholdAdu,
            SpikeThresholdPercentageRedZone = bufferProfile.SpikeThresholdPercentageRedZone,
            IsActive = bufferProfile.IsActive,
            IsMakeToOrder = bufferProfile.IsMakeToOrder
        };

        public static CenterProductGetDto ToGetDto(this CenterProduct centerProduct) => new()
        {
            Id = centerProduct.Id,
            IdProduct = centerProduct.IdProduct,
            IdCenter = centerProduct.IdCenter,
            IdOriginCenter = centerProduct.IdOriginCenter,
            PackQuantity = centerProduct.PackQuantity,
            Moq = centerProduct.Moq,
            LeadTime = centerProduct.LeadTime,
            Frequency = centerProduct.Frequency,
            Class = centerProduct.Class,
            Classification = centerProduct.Classification,
            Segment = centerProduct.Segment,
            Stock = centerProduct.Stock,
            IdProvider = centerProduct.IdProvider,
            IdTag = centerProduct.IdTag,
            IdReason = centerProduct.IdReason,
            IdAllocationGroup = centerProduct.IdAllocationGroup,
            IdBufferProfile = centerProduct.IdBufferProfile,
            Adu = centerProduct.Adu,
            FutureAduDays = centerProduct.FutureAduDays,
            HistoryAduDays = centerProduct.HistoryAduDays,
            Adi = centerProduct.Adi,
            StandardDeviation = centerProduct.StandardDeviation,
            Cv = centerProduct.Cv,
            UseSuggestedLTFactor = centerProduct.UseSuggestedLTFactor,
            UseSuggestedVariabilityFactor = centerProduct.UseSuggestedVariabilityFactor,
            RedZoneBase = centerProduct.RedZoneBase,
            RedZoneSafe = centerProduct.RedZoneSafe,
            RedZone = centerProduct.RedZone,
            YellowZone = centerProduct.YellowZone,
            GreenZone = centerProduct.GreenZone,
            TopOfRed = centerProduct.TopOfRed,
            TopOfYellow = centerProduct.TopOfYellow,
            TopOfGreen = centerProduct.TopOfGreen,
            RedZoneExecution = centerProduct.RedZoneExecution,
            YellowZoneExecution = centerProduct.YellowZoneExecution,
            GreenZoneExecution = centerProduct.GreenZoneExecution,
            UseDafOnGreenZone = centerProduct.UseDafOnGreenZone,
            CustomLeadTimeFactor = centerProduct.CustomLeadTimeFactor,
            CustomVariabilityFactor = centerProduct.CustomVariabilityFactor,
            GreenZoneParametrizationUseMoq = centerProduct.GreenZoneParametrizationUseMoq,
            GreenZoneParametrizationUseAduXFrequency = centerProduct.GreenZoneParametrizationUseAduXFrequency,
            GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = centerProduct.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime,
            BufferType = centerProduct.BufferType,
            ZafRedZone = centerProduct.ZafRedZone,
            ZafYellowZone = centerProduct.ZafYellowZone,
            ZafGreenZone = centerProduct.ZafGreenZone,
            QualifiedDemand = centerProduct.QualifiedDemand,
            SpikeHorizonType = centerProduct.SpikeHorizonType,
            SpikeHorizonValue = centerProduct.SpikeHorizonValue,
            SpikeHorizonLTDays = centerProduct.SpikeHorizonLTDays,
            SpikeThresholdType = centerProduct.SpikeThresholdType,
            SpikeThresholdAdu = centerProduct.SpikeThresholdAdu,
            SpikeThresholdPercentageRedZone = centerProduct.SpikeThresholdPercentageRedZone,
            Product = centerProduct.Product?.ToGetDto(),
            Center = centerProduct.Center?.ToGetDto(),
            OriginCenter = centerProduct.OriginCenter?.ToGetDto(),
            Provider = centerProduct.Provider?.ToGetDto(),
            Tag = centerProduct.Tag?.ToGetDto(),
            Reason = centerProduct.Reason?.ToGetDto(),
            AllocationGroup = centerProduct.AllocationGroup?.ToGetDto(),
            BufferProfile = centerProduct.BufferProfile?.ToGetDto()
        };
    }
}
