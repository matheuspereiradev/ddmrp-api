using Service.Application.DTOs.Forecast;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Application.Mappers;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;

namespace Service.Application.Services
{
    public class ForecastService : BaseService<Forecast, ForecastGetDto, ForecastPostDto, ForecastPutDto>, IForecastService
    {
        private readonly IForecastRepository _forecastRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICenterRepository _centerRepository;

        public ForecastService(IForecastRepository repository, IProductRepository productRepository, ICenterRepository centerRepository)
            : base(repository)
        {
            _forecastRepository = repository;
            _productRepository = productRepository;
            _centerRepository = centerRepository;
        }

        protected override ForecastGetDto ToGetDTO(Forecast entity)
        {
            return new ForecastGetDto
            {
                Id = entity.Id,
                IdProduct = entity.IdProduct,
                IdCenter = entity.IdCenter,
                Value = entity.Value,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Product = entity.Product?.ToGetDto(),
                Center = entity.Center?.ToGetDto()
            };
        }

        protected override Forecast ToEntity(ForecastPostDto postDTO)
        {
            return new Forecast
            {
                IdProduct = postDTO.IdProduct,
                IdCenter = postDTO.IdCenter,
                Value = postDTO.Value,
                StartDate = postDTO.StartDate,
                EndDate = postDTO.EndDate
            };
        }

        protected override void ApplyUpdate(Forecast entity, ForecastPutDto putDTO)
        {
            entity.Value = putDTO.Value;
        }

        public override async Task<ForecastGetDto> AddAsync(ForecastPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            if (postDTO.EndDate < postDTO.StartDate)
                throw new BadRequestException("EndDate must be greater than or equal to StartDate.");

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public async Task<PagedList<ForecastDailyGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _forecastRepository.GetFilteredAsync(idProduct, idCenter, dateStart, dateEnd, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(row => new ForecastDailyGetDto
            {
                Id = row.Id,
                IdProduct = row.IdProduct,
                IdCenter = row.IdCenter,
                Value = row.Value,
                Date = row.Date,
                Product = row.Product?.ToGetDto(),
                Center = row.Center?.ToGetDto()
            }).ToList();
            return new PagedList<ForecastDailyGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public async Task<PagedList<ForecastGetDto>> GetGroupedAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _forecastRepository.GetGroupedAsync(idProduct, idCenter, dateStart, dateEnd, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<ForecastGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }
    }
}
