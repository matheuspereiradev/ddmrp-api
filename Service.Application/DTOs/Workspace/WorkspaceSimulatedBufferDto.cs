using Service.Domain.Enums;

namespace Service.Application.DTOs.Workspace
{
    public class WorkspaceSimulatedBufferDto
    {
        public decimal SimulatedNetflowBufferPercentage { get; set; }
        public BufferColor SimulatedNetflowBufferColor { get; set; }
    }
}
