using Service.Application.DTOs.Workspace;

namespace Service.Application.Interfaces
{
    public interface IWorkspaceService
    {
        Task<WorkspaceSimulatedBufferDto> UpdateWorkspaceAsync(WorkspaceUpdateDto updateDto, CancellationToken cancellationToken = default);
        Task ClearWorkspaceAsync(CancellationToken cancellationToken = default);
    }
}
