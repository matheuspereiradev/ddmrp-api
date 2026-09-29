using System.Text.Json;
using Service.API.Extensions;
using Service.API.Filters;
using Service.API.Models;
using Service.Application.DTOs.Exporter;
using Service.Application.Exceptions;
using Service.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Service.API.Controllers
{
    // Read-only + run — exporters are registered directly in the database, not through this API.
    [ApiController]
    [Route("api/[controller]")]
    public class ExporterController : Controller
    {
        private readonly IExporterService _exporterService;

        public ExporterController(IExporterService exporterService)
        {
            _exporterService = exporterService;
        }

        [HttpGet]
        [Authorize]
        [RequirePermission("exporter")]
        public async Task<ActionResult> GetAllExporters([FromQuery] PaginationParams paginationParams, CancellationToken cancellationToken)
        {
            var exporters = await _exporterService.GetAllAsync(paginationParams.PageNumber, paginationParams.PageSize, cancellationToken);

            Response.AddPaginationHeader(
                new PaginationHeader(paginationParams.PageNumber, paginationParams.PageSize, exporters.TotalCount, exporters.TotalPages));

            return Ok(exporters);
        }

        [HttpGet("{id}")]
        [Authorize]
        [RequirePermission("exporter")]
        public async Task<ActionResult> GetExporter(int id, CancellationToken cancellationToken)
        {
            var exporter = await _exporterService.GetByIdAsync(id, cancellationToken);
            return Ok(exporter);
        }

        [HttpGet("{id}/params")]
        [Authorize]
        [RequirePermission("exporter")]
        public async Task<ActionResult> GetParams(int id, CancellationToken cancellationToken)
        {
            var parameters = await _exporterService.GetParametersAsync(id, cancellationToken);
            return Ok(parameters);
        }

        // No [FromBody] parameter on purpose: most exporters take no parameters at all, and
        // relying on automatic body-model-binding means a request whose Content-Type isn't
        // application/json (missing entirely, or something a caller's tooling defaults to, like
        // Swagger UI's empty multipart/form-data submission for an action with no declared body)
        // fails with 415 before we ever get a chance to treat "no usable body" as "no parameters".
        // We only ever attempt to parse the body when its Content-Type actually says JSON —
        // anything else (including no body at all) is silently treated as an empty parameter set.
        [HttpPost("{id}/run")]
        [Authorize]
        [RequirePermission("exporter")]
        public async Task<ActionResult> Run(int id, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, string?>();

            if (Request.HasJsonContentType())
            {
                ExporterRunRequestDto? dto;
                try
                {
                    dto = await Request.ReadFromJsonAsync<ExporterRunRequestDto>(cancellationToken);
                }
                catch (JsonException ex)
                {
                    throw new BadRequestException($"Invalid JSON body: {ex.Message}");
                }

                parameters = dto?.Parameters ?? [];
            }

            var result = await _exporterService.RunAsync(id, parameters, cancellationToken);
            return Ok(result);
        }

        [HttpGet("download/{fileName}")]
        [Authorize]
        [RequirePermission("exporter")]
        public async Task<ActionResult> Download(string fileName, CancellationToken cancellationToken)
        {
            var stream = await _exporterService.OpenDownloadStreamAsync(fileName, cancellationToken);
            return File(stream, "text/csv", fileName);
        }
    }
}
