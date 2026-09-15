using Service.Application.DTOs.Workspace;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Enums;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class WorkspaceService : IWorkspaceService
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly IProductRepository _productRepository;
        private readonly IReportRepository _reportRepository;
        private readonly ICurrentUserService _currentUser;

        public WorkspaceService(
            IWorkspaceRepository workspaceRepository,
            ICenterRepository centerRepository,
            IProductRepository productRepository,
            IReportRepository reportRepository,
            ICurrentUserService currentUser)
        {
            _workspaceRepository = workspaceRepository;
            _centerRepository = centerRepository;
            _productRepository = productRepository;
            _reportRepository = reportRepository;
            _currentUser = currentUser;
        }

        public async Task<WorkspaceSimulatedBufferDto> UpdateWorkspaceAsync(WorkspaceUpdateDto updateDto, CancellationToken cancellationToken = default)
        {
            if (!await _centerRepository.Exists(updateDto.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            if (!await _productRepository.Exists(updateDto.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            var idUser = _currentUser.UserId;
            var existing = await _workspaceRepository.GetByKeyAsync(updateDto.IdCenter, updateDto.IdProduct, idUser, cancellationToken);

            Workspace workspace;
            if (existing == null)
            {
                workspace = new Workspace
                {
                    IdCenter = updateDto.IdCenter,
                    IdProduct = updateDto.IdProduct,
                    IdUser = idUser,
                    OptimizedQuantity = updateDto.OptimizedQuantity,
                    Approved = updateDto.Approved
                };
                workspace = await _workspaceRepository.AddAsync(workspace, cancellationToken);
            }
            else
            {
                existing.OptimizedQuantity = updateDto.OptimizedQuantity;
                existing.Approved = updateDto.Approved;
                workspace = await _workspaceRepository.UpdateAsync(existing, cancellationToken);
            }

            var bufferRow = await _reportRepository.GetInventoryBufferManagementRowAsync(workspace.IdCenter, workspace.IdProduct, cancellationToken);

            return new WorkspaceSimulatedBufferDto
            {
                SimulatedNetflowBufferPercentage = bufferRow?.SimulatedNetflowBufferPercentage ?? 0,
                SimulatedNetflowBufferColor = bufferRow?.SimulatedNetflowBufferColor ?? BufferColor.NoColor
            };
        }

        public async Task ClearWorkspaceAsync(CancellationToken cancellationToken = default)
        {
            await _workspaceRepository.ClearByUserAsync(_currentUser.UserId, cancellationToken);
        }
    }
}
