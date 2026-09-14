using Microsoft.OpenApi;
using Service.API.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Service.API.Swagger
{
    public class ApiResponseWrapperOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
            {
                var statusCode = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();

                if (!operation.Responses.TryGetValue(statusCode, out var response) || response.Content == null)
                    continue;

                if (!response.Content.TryGetValue("application/json", out var mediaType))
                    continue;

                var dataType = responseType.Type is null || responseType.Type == typeof(void)
                    ? typeof(object)
                    : responseType.Type;

                var wrapperType = typeof(ApiResponseDto<>).MakeGenericType(dataType);
                mediaType.Schema = context.SchemaGenerator.GenerateSchema(wrapperType, context.SchemaRepository);
            }
        }
    }
}
