using Service.Application.DTOs.BufferProfile;
using Service.Domain.Entities;

namespace Service.Application.Interfaces
{
    public interface IBufferProfileService : IBaseService<BufferProfile, BufferProfileGetDto, BufferProfilePostDto, BufferProfilePutDto>
    {
        Task<BufferProfileGetDto> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    }
}
