using Service.Application.DTOs.AllocationGroup;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Account;
using Service.Domain.AllocationGroups;
using Service.Domain.Entities;
using Service.Domain.Interfaces;

namespace Service.Application.Services
{
    public class AllocationGroupService : BaseService<AllocationGroup, AllocationGroupGetDto, AllocationGroupPostDto, AllocationGroupPutDto>, IAllocationGroupService
    {
        private readonly IAllocationGroupRepository _allocationGroupRepository;
        private readonly IReportRepository _reportRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ICurrentUserService _currentUser;

        public AllocationGroupService(
            IAllocationGroupRepository repository,
            IReportRepository reportRepository,
            IWorkspaceRepository workspaceRepository,
            ICurrentUserService currentUser) : base(repository)
        {
            _allocationGroupRepository = repository;
            _reportRepository = reportRepository;
            _workspaceRepository = workspaceRepository;
            _currentUser = currentUser;
        }

        public Task<List<PriorizedAllocationRow>> GetPriorizedAllocationAsync(CancellationToken cancellationToken = default)
        {
            return _allocationGroupRepository.GetPriorizedAllocationAsync(_currentUser.UserId, cancellationToken);
        }

        public async Task<List<PriorizedAllocationItem>> RunPriorizedAllocationAsync(PriorizedAllocationRunDto runDto, CancellationToken cancellationToken = default)
        {
            if (!await _allocationGroupRepository.Exists(runDto.IdGroup, cancellationToken))
                throw new NotFoundException("Allocation group not found.");

            var rows = await _reportRepository.GetApprovedByAllocationGroupAsync(runDto.IdGroup, cancellationToken);

            var items = rows.Select(r => new PriorizedAllocationItem
            {
                Id = r.Id,
                IdCenter = r.IdCenter,
                IdProduct = r.IdProduct,
                ApprovedQuantity = r.OptimizedOrderQuantity,
                Netflow = r.Netflow,
                Tog = r.TopOfGreen ?? 0,
                Moq = r.Moq,
                PackQuantity = r.PackQuantity,
                ProductWeight = r.ProductWeight,
                ProductVolume = r.ProductVolume,
                ProductValue = r.ProductValue,
                ProductPallet = r.ProductPallet
            }).ToList();

            if (items.Count == 0)
                return items;

            PriorizedAllocationEngine.Run(items, runDto.Limit, runDto.StopCondition, runDto.AdjustmentType);

            var currentUserId = _currentUser.UserId;
            foreach (var item in items)
            {
                var workspace = await _workspaceRepository.GetByKeyAsync(item.IdCenter, item.IdProduct, currentUserId, cancellationToken);
                if (workspace == null)
                    continue;

                workspace.OptimizedQuantity = item.ApprovedQuantity;
                await _workspaceRepository.UpdateAsync(workspace, cancellationToken);
            }

            return items;
        }

        protected override AllocationGroupGetDto ToGetDTO(AllocationGroup entity) => entity.ToGetDto();

        protected override AllocationGroup ToEntity(AllocationGroupPostDto postDTO)
        {
            return new AllocationGroup
            {
                Name = postDTO.Name
            };
        }

        protected override void ApplyUpdate(AllocationGroup entity, AllocationGroupPutDto putDTO)
        {
            entity.Name = putDTO.Name;
        }
    }
}
