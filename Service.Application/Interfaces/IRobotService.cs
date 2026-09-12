using Service.Application.DTOs.Robot;

namespace Service.Application.Interfaces
{
    public interface IRobotService
    {
        Task<RobotRunResultDto> RunAsync(CancellationToken cancellationToken = default);
    }
}
