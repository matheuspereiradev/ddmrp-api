using Service.Application.DTOs.Holiday;
using Service.Domain.Pagination;

namespace Service.Application.Interfaces
{
    public interface IHolidayService
    {
        Task<List<HolidayGetDto>> AddAsync(HolidayPostDto postDto, CancellationToken cancellationToken = default);
        Task<PagedList<HolidayGetDto>> GetAllAsync(int pageNumber, int pageSize, bool includePast = false, CancellationToken cancellationToken = default);
        Task<HolidayGetDto> UpdateAsync(int id, HolidayPutDto putDto, CancellationToken cancellationToken = default);
        Task<HolidayGetDto> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
