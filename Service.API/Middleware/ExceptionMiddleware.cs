using Service.API.Models;
using Service.Application.Exceptions;

namespace Service.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                bool isExpected = ex is AppException or UnauthorizedAccessException;

                if (isExpected)
                    _logger.LogWarning("{Message}", ex.Message);
                else
                    _logger.LogError(ex, ex.Message);

                int statusCode = ex switch
                {
                    AppException appException => appException.StatusCode,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    _ => StatusCodes.Status500InternalServerError
                };

                string[] errors = ex switch
                {
                    AppException appException => [appException.Message],
                    UnauthorizedAccessException => [ex.Message],
                    _ => ["Internal server error"]
                };

                httpContext.Response.StatusCode = statusCode;
                httpContext.Response.ContentType = "application/json";

                var response = ApiResponseDto<object?>.Fail(errors);

                await httpContext.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
