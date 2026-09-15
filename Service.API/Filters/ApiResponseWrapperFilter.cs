using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Service.API.Models;

namespace Service.API.Filters
{
    public class ApiResponseWrapperFilter : IAsyncResultFilter
    {
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            // OData routes (e.g. ReportController.InventoryBufferManagement) return their own
            // value/@odata.count shape for TanStack Table + odata-query on the frontend — never
            // wrap those in ApiResponseDto<T>. Marked explicitly with [SkipApiResponseWrapper]
            // rather than detecting OData by attribute, since that route builds its response
            // manually via ODataQueryOptions<T> instead of [EnableQuery] (see ReportController —
            // [EnableQuery] alone doesn't produce the {value, @odata.count} envelope outside
            // conventional OData routing with a registered EDM model).
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is SkipApiResponseWrapperAttribute))
                return next();

            switch (context.Result)
            {
                case ObjectResult objectResult:
                    objectResult.Value = Wrap(objectResult.Value, objectResult.StatusCode ?? StatusCodes.Status200OK);
                    break;

                case StatusCodeResult statusCodeResult:
                    context.Result = new ObjectResult(ApiResponseDto<object?>.Ok(null))
                    {
                        StatusCode = statusCodeResult.StatusCode
                    };
                    break;
            }

            return next();
        }

        private static object Wrap(object? value, int statusCode)
        {
            if (value is ValidationProblemDetails validationProblem)
            {
                var errors = validationProblem.Errors.SelectMany(e => e.Value).ToArray();
                return ApiResponseDto<object?>.Fail(errors);
            }

            if (value is ProblemDetails problemDetails)
            {
                return ApiResponseDto<object?>.Fail(problemDetails.Detail ?? problemDetails.Title ?? "Error");
            }

            if (statusCode is >= 200 and < 300)
            {
                return ApiResponseDto<object?>.Ok(value);
            }

            return ApiResponseDto<object?>.Fail(value?.ToString() ?? "Error");
        }
    }
}
