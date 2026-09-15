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
                Quantity = entity.Quantity,
                Date = entity.Date,
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
                Quantity = postDTO.Quantity,
                Date = postDTO.Date
            };
        }

        protected override void ApplyUpdate(Forecast entity, ForecastPutDto putDTO)
        {
            entity.Quantity = putDTO.Quantity;
        }

        public override async Task<ForecastGetDto> AddAsync(ForecastPostDto postDTO, CancellationToken cancellationToken = default)
        {
            if (!await _productRepository.Exists(postDTO.IdProduct, cancellationToken))
                throw new BadRequestException("Product not found.");

            if (!await _centerRepository.Exists(postDTO.IdCenter, cancellationToken))
                throw new BadRequestException("Center not found.");

            return await base.AddAsync(postDTO, cancellationToken);
        }

        public async Task<PagedList<ForecastGetDto>> GetFilteredAsync(int? idProduct, int? idCenter, DateTime? dateStart, DateTime? dateEnd, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _forecastRepository.GetFilteredAsync(idProduct, idCenter, dateStart, dateEnd, pageNumber, pageSize, cancellationToken);
            var items = paged.Select(ToGetDTO).ToList();
            return new PagedList<ForecastGetDto>(items, paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }
    }
}
