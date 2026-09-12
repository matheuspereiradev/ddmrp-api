using Service.Application.DTOs.Partner;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class PartnerService : BaseService<Partner, PartnerGetDto, PartnerPostDto, PartnerPutDto>, IPartnerService
    {
        public PartnerService(IPartnerRepository repository) : base(repository)
        {
        }

        protected override PartnerGetDto ToGetDTO(Partner entity) => entity.ToGetDto();

        protected override Partner ToEntity(PartnerPostDto postDTO)
        {
            return new Partner
            {
                Code = postDTO.Code,
                Description = postDTO.Description
            };
        }

        protected override void ApplyUpdate(Partner entity, PartnerPutDto putDTO)
        {
            entity.Code = putDTO.Code;
            entity.Description = putDTO.Description;
        }
    }
}
