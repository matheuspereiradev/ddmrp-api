using Service.Application.DTOs.MasterBuffer;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IMasterBufferService : IBaseService<MasterBuffer, MasterBufferGetDto, MasterBufferPostDto, MasterBufferPutDto>
    {
    }
}
