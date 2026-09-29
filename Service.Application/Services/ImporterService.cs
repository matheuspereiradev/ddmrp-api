using Service.Application.DTOs.Importer;
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
    // Read-only + run — see IImporterService. Deliberately not a BaseService: there is no
    // create/update/delete to implement, and inheriting BaseService would force a PostDto/PutDto
    // pair that nothing would ever call.
    public class ImporterService : IImporterService
    {
        private readonly IImporterRepository _importerRepository;
        private readonly IProcedureCatalogService _procedureCatalog;
        private readonly IImportProcedureRunner _importRunner;
        private readonly ICurrentUserService _currentUser;

        public ImporterService(
            IImporterRepository importerRepository,
            IProcedureCatalogService procedureCatalog,
            IImportProcedureRunner importRunner,
            ICurrentUserService currentUser)
        {
            _importerRepository = importerRepository;
            _procedureCatalog = procedureCatalog;
            _importRunner = importRunner;
            _currentUser = currentUser;
        }

        public async Task<ImporterGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var importer = await _importerRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Importer not found.");
            return ToGetDto(importer);
        }

        public async Task<PagedList<ImporterGetDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var paged = await _importerRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);
            return new PagedList<ImporterGetDto>(paged.Select(ToGetDto).ToList(), paged.CurrentPage, paged.PageSize, paged.TotalCount);
        }

        public async Task<List<ProcedureParameterDto>> GetParametersAsync(int id, CancellationToken cancellationToken = default)
        {
            var importer = await _importerRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Importer not found.");
            var parameters = await _procedureCatalog.GetParametersAsync(importer.ProcedureName, cancellationToken);
            return parameters.Select(ToParameterDto).ToList();
        }

        public async Task<ImporterRunResultDto> RunAsync(int id, Stream csvStream, CancellationToken cancellationToken = default)
        {
            var importer = await _importerRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Importer not found.");

            try
            {
                var result = await _importRunner.RunAsync(importer, csvStream, _currentUser.UserId, cancellationToken);
                return new ImporterRunResultDto { RowCount = result.RowCount };
            }
            catch (InvalidOperationException ex)
            {
                throw new BadRequestException(ex.Message);
            }
        }

        private static ImporterGetDto ToGetDto(Importer entity) => new()
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
