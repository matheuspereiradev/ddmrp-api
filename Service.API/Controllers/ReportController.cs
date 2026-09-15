using Service.API.Filters;
using Service.API.Models;
using Service.Application.Interfaces;
using Service.Domain.Report.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Query.Validator;
using Microsoft.EntityFrameworkCore;

namespace Service.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : Controller
    {
        private static readonly ODataValidationSettings InventoryBufferManagementValidationSettings = new()
        {
            MaxTop = 500,
            AllowedQueryOptions = AllowedQueryOptions.Filter | AllowedQueryOptions.OrderBy | AllowedQueryOptions.Top | AllowedQueryOptions.Skip | AllowedQueryOptions.Count
        };

        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // OData instead of the usual ApiResponseDto<T> envelope — this route is meant to be consumed by
        // TanStack Table + odata-query on the frontend with $filter/$orderby/$top/$skip/$count running in SQL,
        // since the underlying table can have up to ~1M CenterProduct rows and materializing everything per
        // request isn't viable. See CLAUDE.md's Report module bullet for the full rationale.
        //
        // [EnableQuery] is NOT used here (2026-09-15, a real bug found on first manual test): outside
        // conventional OData routing with a registered EDM model, [EnableQuery] applies the query options
        // fine but the response comes back as a bare JSON array — no {value, @odata.count} envelope, because
        // that envelope is written by the OData-specific output formatter, which only activates for requests
        // matched to an OData route. Registering a full EDM model would fix the envelope but switches the
        // whole response to the OData formatter's own serialization (PascalCase property names, no
        // JsonStringEnumConverter/DecimalRoundingJsonConverter) — a much bigger break to the contract already
        // documented for the frontend. Instead, ODataQueryOptions<T> is bound directly (still real OData
        // $filter/$orderby parsing and translation to SQL — same ApplyTo mechanism [EnableQuery] uses
        // internally) and the {value, @odata.count} shape is built by hand via ODataResult<T>, going through
        // the app's normal System.Text.Json pipeline — camelCase and every converter still apply.
        // [SkipApiResponseWrapper] replaces [EnableQuery] as the marker ApiResponseWrapperFilter checks for.
        [HttpGet("inventoryBufferManagement")]
        [Authorize]
        [SkipApiResponseWrapper]
        public async Task<ActionResult<ODataResult<InventoryBufferManagementRow>>> InventoryBufferManagement(
            ODataQueryOptions<InventoryBufferManagementRow> queryOptions,
            CancellationToken cancellationToken)
        {
            queryOptions.Validate(InventoryBufferManagementValidationSettings);

            var query = _reportService.GetInventoryBufferManagementQueryable();
            var settings = new ODataQuerySettings();

            if (queryOptions.Filter != null)
                query = (IQueryable<InventoryBufferManagementRow>)queryOptions.Filter.ApplyTo(query, settings);

            long? count = null;
            if (queryOptions.Count?.Value == true)
                count = await query.LongCountAsync(cancellationToken);

            if (queryOptions.OrderBy != null)
                query = (IQueryable<InventoryBufferManagementRow>)queryOptions.OrderBy.ApplyTo(query, settings);

            if (queryOptions.Skip != null)
                query = (IQueryable<InventoryBufferManagementRow>)queryOptions.Skip.ApplyTo(query, settings);

            if (queryOptions.Top != null)
                query = (IQueryable<InventoryBufferManagementRow>)queryOptions.Top.ApplyTo(query, settings);

            var rows = await query.ToListAsync(cancellationToken);
            return Ok(new ODataResult<InventoryBufferManagementRow> { Value = rows, Count = count });
        }

        [HttpGet("openOrders/inbounds")]
        [Authorize]
        public async Task<ActionResult> OpenOrders([FromQuery] int? idCenter, [FromQuery] int? idProduct, CancellationToken cancellationToken)
        {
            var result = await _reportService.GetOpenOrdersAsync(idCenter, idProduct, cancellationToken);
            return Ok(result);
        }
    }
}
