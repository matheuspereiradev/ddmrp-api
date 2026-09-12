using Service.Application.DTOs.Center;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class CenterService : BaseService<Center, CenterGetDto, CenterPostDto, CenterPutDto>, ICenterService
    {
        public CenterService(ICenterRepository repository) : base(repository)
        {
        }

        protected override CenterGetDto ToGetDTO(Center entity) => entity.ToGetDto();

        protected override Center ToEntity(CenterPostDto postDTO)
        {
            return new Center
            {
                Code = postDTO.Code,
                Description = postDTO.Description,
                City = postDTO.City,
                Zone = postDTO.Zone
            };
        }

        protected override void ApplyUpdate(Center entity, CenterPutDto putDTO)
        {
            entity.Code = putDTO.Code;
            entity.Description = putDTO.Description;
            entity.City = putDTO.City;
            entity.Zone = putDTO.Zone;
        }
    }
}
