using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    // Read-only + run — importers are registered directly in the database, not through this API.
    [ApiController]
    [Route("api/[controller]")]
    public class ImporterController : Controller
    {
        private readonly IImporterService _importerService;

        public ImporterController(IImporterService importerService)
        {
            _importerService = importerService;
        }

        [HttpGet]
        [Authorize]
        [RequirePermission("importer")]
        public async Task<ActionResult> GetAllImporters([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var importers = await _importerService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, importers.TotalCount, importers.TotalPages));

            return Ok(importers);
        }

        [HttpGet("{id}")]
        [Authorize]
        [RequirePermission("importer")]
        public async Task<ActionResult> GetImporter(int id, CancellationToken cancellationToken)
        {
            var importer = await _importerService.GetByIdAsync(id, cancellationToken);
            return Ok(importer);
        }

        [HttpGet("{id}/params")]
        [Authorize]
        [RequirePermission("importer")]
        public async Task<ActionResult> GetParams(int id, CancellationToken cancellationToken)
        {
            var parameters = await _importerService.GetParametersAsync(id, cancellationToken);
            return Ok(parameters);
        }

        [HttpPost("{id}/run")]
        [Authorize]
        [RequirePermission("importer")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> Run(int id, IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                throw new BadRequestException("A CSV file is required.");

            if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Only .csv files are accepted.");

            await using var stream = file.OpenReadStream();
            var result = await _importerService.RunAsync(id, stream, cancellationToken);
            return Ok(result);
        }
    }
}
