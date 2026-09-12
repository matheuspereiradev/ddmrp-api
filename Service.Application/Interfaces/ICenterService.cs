using Service.Application.DTOs.Center;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface ICenterService : IBaseService<Center, CenterGetDto, CenterPostDto, CenterPutDto>
    {
    }
}
