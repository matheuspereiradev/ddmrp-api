using Service.Application.DTOs.ReasonGroup;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class ReasonGroupService : IReasonGroupService
    {
        private readonly IReasonGroupRepository _reasonGroupRepository;

        public ReasonGroupService(IReasonGroupRepository reasonGroupRepository)
        {
            _reasonGroupRepository = reasonGroupRepository;
        }

        public async Task<PagedList<ReasonGroupGetDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _reasonGroupRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
            var items = paged.Select(rg => rg.ToGetDto()).ToList();
            return new PagedList<ReasonGroupGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }
    }
}
