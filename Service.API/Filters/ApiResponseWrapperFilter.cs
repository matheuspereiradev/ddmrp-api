using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Service.API.Models;

namespace Service.API.Filters
{
    public class ApiResponseWrapperFilter : IAsyncResultFilter
    {
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
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
