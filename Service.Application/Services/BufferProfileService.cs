using Service.Application.DTOs.BufferProfile;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class BufferProfileService : BaseService<BufferProfile, BufferProfileGetDto, BufferProfilePostDto, BufferProfilePutDto>, IBufferProfileService
    {
        public BufferProfileService(IBufferProfileRepository repository) : base(repository)
        {
        }

        protected override BufferProfileGetDto ToGetDTO(BufferProfile entity) => entity.ToGetDto();

        protected override BufferProfile ToEntity(BufferProfilePostDto postDTO)
        {
            return new BufferProfile
            {
                ProfileName = postDTO.ProfileName,
                SupplyType = postDTO.SupplyType,
                LeadTimeCategory = postDTO.LeadTimeCategory,
                VariabilityCategory = postDTO.VariabilityCategory,
                LeadTimeFactor = postDTO.LeadTimeFactor,
                VariabilityFactor = postDTO.VariabilityFactor,
                AduCalculationDays = postDTO.AduCalculationDays,
                AduFutureDays = postDTO.AduFutureDays,
                Frequency = postDTO.Frequency,
                GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = postDTO.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime,
                GreenZoneParametrizationUseMoq = postDTO.GreenZoneParametrizationUseMoq,
                GreenZoneParametrizationUseAduXFrequency = postDTO.GreenZoneParametrizationUseAduXFrequency,
                SpikeHorizonType = postDTO.SpikeHorizonType,
                SpikeHorizonValue = postDTO.SpikeHorizonValue,
                SpikeHorizonLTDays = postDTO.SpikeHorizonLTDays,
                SpikeThresholdType = postDTO.SpikeThresholdType,
                SpikeThresholdAdu = postDTO.SpikeThresholdAdu,
                SpikeThresholdPercentageRedZone = postDTO.SpikeThresholdPercentageRedZone,
                IsActive = postDTO.IsActive,
                IsMakeToOrder = postDTO.IsMakeToOrder
            };
        }

        protected override void ApplyUpdate(BufferProfile entity, BufferProfilePutDto putDTO)
        {
            entity.ProfileName = putDTO.ProfileName;
            entity.SupplyType = putDTO.SupplyType;
            entity.LeadTimeCategory = putDTO.LeadTimeCategory;
            entity.VariabilityCategory = putDTO.VariabilityCategory;
            entity.LeadTimeFactor = putDTO.LeadTimeFactor;
            entity.VariabilityFactor = putDTO.VariabilityFactor;
            entity.AduCalculationDays = putDTO.AduCalculationDays;
            entity.AduFutureDays = putDTO.AduFutureDays;
            entity.Frequency = putDTO.Frequency;
            entity.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime = putDTO.GreenZoneParametrizationUseAduXLeadTimeXFactLeadTime;
            entity.GreenZoneParametrizationUseMoq = putDTO.GreenZoneParametrizationUseMoq;
            entity.GreenZoneParametrizationUseAduXFrequency = putDTO.GreenZoneParametrizationUseAduXFrequency;
            entity.SpikeHorizonType = putDTO.SpikeHorizonType;
            entity.SpikeHorizonValue = putDTO.SpikeHorizonValue;
            entity.SpikeHorizonLTDays = putDTO.SpikeHorizonLTDays;
            entity.SpikeThresholdType = putDTO.SpikeThresholdType;
            entity.SpikeThresholdAdu = putDTO.SpikeThresholdAdu;
            entity.SpikeThresholdPercentageRedZone = putDTO.SpikeThresholdPercentageRedZone;
            entity.IsActive = putDTO.IsActive;
            entity.IsMakeToOrder = putDTO.IsMakeToOrder;
        }

        public async Task<BufferProfileGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
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
