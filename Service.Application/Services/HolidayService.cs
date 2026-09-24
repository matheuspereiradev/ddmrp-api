using Service.Application.DTOs.Holiday;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly IHolidayRepository _holidayRepository;

        public HolidayService(IHolidayRepository holidayRepository)
        {
            _holidayRepository = holidayRepository;
        }

        public async Task<List<HolidayGetDto>> AddAsync(HolidayPostDto postDto, CancellationToken cancellationToken = default)
        {
            var date = postDto.Date.Date;

            if (!postDto.IsRecurring)
            {
                var created = await _holidayRepository.AddAsync(new Holiday { Name = postDto.Name, Date = date }, cancellationToken);
                return [created.ToGetDto()];
            }

            var holidays = Enumerable.Range(0, 15)
                .Select(offset => new Holiday { Name = postDto.Name, Date = date.AddYears(offset) })
                .ToList();

            var createdHolidays = await _holidayRepository.AddRangeAsync(holidays, cancellationToken);
            return createdHolidays.Select(h => h.ToGetDto()).ToList();
        }

        public async Task<PagedList<HolidayGetDto>> GetAllAsync(int pageNumber, int pageSize, bool includePast = false, CancellationToken cancellationToken = default)
        {
            var paged = await _holidayRepository.GetFilteredAsync(pageNumber, pageSize, includePast, cancellationToken);
            var items = paged.Select(h => h.ToGetDto()).ToList();
            return new PagedList<HolidayGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public async Task<HolidayGetDto> UpdateAsync(int id, HolidayPutDto putDto, CancellationToken cancellationToken = default)
        {
            var entity = await _holidayRepository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                throw new NotFoundException("Holiday not found.");

            entity.Name = putDto.Name;
            entity.Date = putDto.Date.Date;

            var updated = await _holidayRepository.UpdateAsync(entity, cancellationToken);
            return updated.ToGetDto();
        }

        public async Task<HolidayGetDto> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var deleted = await _holidayRepository.DeleteAsync(id, cancellationToken);
            if (deleted == null)
                throw new NotFoundException("Holiday not found.");
            return deleted.ToGetDto();
        }
    }
}
