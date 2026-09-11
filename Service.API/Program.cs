using Service.API.Filters;
using Service.API.Middleware;
using Service.Infra.Ioc;
using DotNetEnv;
using Microsoft.OpenApi;

Env.Load();

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseWrapperFilter>();
});
builder.Services.AddInfrastructure(builder.Configuration);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
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

app.MapControllers();

app.Run();
