using Service.Application.DTOs.Partner;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IPartnerService : IBaseService<Partner, PartnerGetDto, PartnerPostDto, PartnerPutDto>
    {
    }
}
