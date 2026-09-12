using Service.Application.DTOs.Reason;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IReasonService : IBaseService<Reason, ReasonGetDto, ReasonPostDto, ReasonPutDto>
    {
    }
}
