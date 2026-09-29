using Service.Application.DTOs.Exporter;
using Service.Application.DTOs.Procedure;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Service.Domain.Account;
using Service.Domain.Entities;
using Service.Domain.Interfaces;
using Service.Domain.Pagination;
using Service.Domain.Procedures;

namespace Service.Application.Services
{
    // Read-only + run — see IExporterService. Deliberately not a BaseService, same reasoning as
    // ImporterService.
    public class ExporterService : IExporterService
    {
        private readonly IExporterRepository _exporterRepository;
        private readonly IProcedureCatalogService _procedureCatalog;
        private readonly IExportProcedureRunner _exportRunner;
        private readonly ICurrentUserService _currentUser;
        private readonly string _exportsDirectory;
        private readonly int _retentionHours;

        public ExporterService(
            IExporterRepository exporterRepository,
            IProcedureCatalogService procedureCatalog,
            IExportProcedureRunner exportRunner,
            ICurrentUserService currentUser,
            string exportsDirectory,
            int retentionHours)
        {
            _exporterRepository = exporterRepository;
            _procedureCatalog = procedureCatalog;
            _exportRunner = exportRunner;
            _currentUser = currentUser;
            _exportsDirectory = Path.IsPathRooted(exportsDirectory) ? exportsDirectory : Path.Combine(Directory.GetCurrentDirectory(), exportsDirectory);
            _retentionHours = retentionHours;
        }

        public async Task<ExporterGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var exporter = await _exporterRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Exporter not found.");
            return ToGetDto(exporter);
        }

        public async Task<PagedList<ExporterGetDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _exporterRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
            return new PagedList<ExporterGetDto>(paged.Select(ToGetDto).ToList(), paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public async Task<List<ProcedureParameterDto>> GetParametersAsync(int id, CancellationToken cancellationToken = default)
        {
            var exporter = await _exporterRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Exporter not found.");
            var parameters = await _procedureCatalog.GetParametersAsync(exporter.ProcedureName, cancellationToken);
            return parameters.Select(ToParameterDto).ToList();
        }

        public async Task<ExporterRunResultDto> RunAsync(int id, Dictionary<string, string?> parameters, CancellationToken cancellationToken = default)
        {
            var exporter = await _exporterRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Exporter not found.");

            Directory.CreateDirectory(_exportsDirectory);
            var fileName = $"{Guid.NewGuid()}.csv";
            var filePath = Path.Combine(_exportsDirectory, fileName);

            int rowCount;
            try
            {
                await using var fileStream = File.Create(filePath);
                rowCount = await _exportRunner.RunAsync(exporter, parameters, _currentUser.UserId, fileStream, cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                throw new BadRequestException(ex.Message);
            }

            return new ExporterRunResultDto
            {
                DownloadUrl = $"/api/exporter/download/{fileName}",
                RowCount = rowCount,
                ExpiresAt = DateTime.UtcNow.AddHours(_retentionHours)
            };
        }

        public Task<Stream> OpenDownloadStreamAsync(string fileName, CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out _) ||
                !string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Invalid export file token.");

            var filePath = Path.Combine(_exportsDirectory, fileName);
            if (!File.Exists(filePath))
                throw new NotFoundException("Export file not found or expired.");

            return Task.FromResult<Stream>(File.OpenRead(filePath));
        }

        private static ExporterGetDto ToGetDto(Exporter entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ProcedureName = entity.ProcedureName
        };

        private static ProcedureParameterDto ToParameterDto(ProcedureParameterInfo info) => new()
        {
            Name = info.Name,
            SqlType = info.SqlTypeName,
            MaxLength = info.MaxLength,
            IsNullable = info.IsNullable,
            IsTableType = info.IsTableType,
            Columns = info.Columns?.Select(c => new ProcedureParameterColumnDto
            {
                Name = c.Name,
                SqlType = c.SqlTypeName,
                MaxLength = c.MaxLength,
                IsNullable = c.IsNullable
            }).ToList()
        };
    }
}
