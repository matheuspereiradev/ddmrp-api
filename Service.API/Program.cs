using Service.API.Filters;
using Service.API.HealthChecks;
using Service.API.Json;
using Service.API.Middleware;
using Service.API.Swagger;
using Service.Infra.Ioc;
using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using System.Text.Json.Serialization;

Env.Load();

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseWrapperFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new DecimalRoundingJsonConverter());
});
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length == 0)
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins(allowedOrigins);

        policy.AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });

    options.OperationFilter<ApiResponseWrapperOperationFilter>();
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();
