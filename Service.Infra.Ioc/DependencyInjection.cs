using Service.Application.Interfaces;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Interfaces;
using Service.Infra.Data.Ai;
using Service.Infra.Data.Calculation;
using Service.Infra.Data.Calculation.Steps;
using Service.Infra.Data.Context;
using Service.Infra.Data.Identity;
using Service.Infra.Data.Ingestion;
using Service.Infra.Data.Ingestion.Writers;
using Service.Infra.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Infra.Ioc
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                   b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
                opt.MapInboundClaims = false;
                opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"])),
                    ClockSkew = TimeSpan.Zero
                };
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ICenterRepository, CenterRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IForecastRepository, ForecastRepository>();
            services.AddScoped<IHistoryRepository, HistoryRepository>();
            services.AddScoped<IPartnerRepository, PartnerRepository>();
            services.AddScoped<ITagRepository, TagRepository>();
            services.AddScoped<IReasonRepository, ReasonRepository>();
            services.AddScoped<IAllocationGroupRepository, AllocationGroupRepository>();
            services.AddScoped<ICenterProductRepository, CenterProductRepository>();
            services.AddScoped<IMasterBufferRepository, MasterBufferRepository>();
            services.AddScoped<IZoneAdjustmentFactorRepository, ZoneAdjustmentFactorRepository>();
            services.AddScoped<IBufferAdjustmentFactorRepository, BufferAdjustmentFactorRepository>();
            services.AddScoped<IDemandAdjustmentFactorRepository, DemandAdjustmentFactorRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IBufferProfileRepository, BufferProfileRepository>();
            services.AddScoped<INoteRepository, NoteRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IReportRepository, ReportRepository>();
            services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
            services.AddScoped<ISettingRepository, SettingRepository>();
            services.AddScoped<IPermissionRepository, PermissionRepository>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ICenterService, CenterService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IForecastService, ForecastService>();
            services.AddScoped<IHistoryService, HistoryService>();
            services.AddScoped<IPartnerService, PartnerService>();
            services.AddScoped<ITagService, TagService>();
            services.AddScoped<IReasonService, ReasonService>();
            services.AddScoped<IAllocationGroupService, AllocationGroupService>();
            services.AddScoped<ICenterProductService, CenterProductService>();
            services.AddScoped<IMasterBufferService, MasterBufferService>();
            services.AddScoped<IZoneAdjustmentFactorService, ZoneAdjustmentFactorService>();
            services.AddScoped<IBufferAdjustmentFactorService, BufferAdjustmentFactorService>();
            services.AddScoped<IDemandAdjustmentFactorService, DemandAdjustmentFactorService>();
            services.AddScoped<IBufferProfileService, BufferProfileService>();
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            services.AddScoped<IPermissionService, PermissionService>();

            services.AddScoped<IAuthenticate, AuthenticateProvider>();
            services.AddScoped<IAuthenticateService, AuthenticateService>();

            services.AddScoped<IIngestionConfigProvider>(_ =>
                new JsonIngestionConfigProvider(configuration["Ingestion:ConfigPath"] ?? "ingestion.config.json"));
            services.AddScoped<IIngestionSourceReader, CsvIngestionSourceReader>();
            services.AddScoped<ILookupValueProvider, LookupValueProvider>();
            services.AddScoped<IIngestionWriter, CenterIngestionWriter>();
            services.AddScoped<IIngestionWriter, ProductIngestionWriter>();
            services.AddScoped<IIngestionWriter, CenterProductIngestionWriter>();
            services.AddScoped<IIngestionWriter, ForecastIngestionWriter>();
            services.AddScoped<IIngestionWriter, HistoryIngestionWriter>();
            services.AddScoped<IIngestionWriter, OrderIngestionWriter>();
            services.AddScoped<IIngestionWriter, GenericTableIngestionWriter>();
            services.AddScoped<IIngestionService, IngestionService>();

            services.AddScoped<ICalculationConfigProvider>(_ =>
                new JsonCalculationConfigProvider(configuration["Calculation:ConfigPath"] ?? "calculation.config.json"));
            services.AddScoped<ICalculationStep, CalculateAduStandardDesvAndCvStep>();
            services.AddScoped<ICalculationStep, CalculateAdiStep>();
            services.AddScoped<ICalculationStep, ApplyBafStep>();
            services.AddScoped<ICalculationStep, CalculateNormalBufferZonesStep>();
            services.AddScoped<ICalculationStep, CalculateMinMaxBufferZonesStep>();
            services.AddScoped<ICalculationStep, CalculateDynamicMinMaxBufferZonesStep>();
            services.AddScoped<ICalculationStep, ApplyZafStep>();
            services.AddScoped<ICalculationStep, CalculateQualifiedDemandStep>();
            services.AddScoped<ICalculationStep, ReplicateCenterProductToHistoryStep>();
            services.AddScoped<ICalculationService, CalculationService>();

            services.AddScoped<IReportService, ReportService>();

            services.AddScoped<IRobotService, RobotService>();

            services.AddSingleton(_ =>
            {
                var client = new HttpClient();
                var baseUrl = configuration["AiConnector:Url"];
                if (!string.IsNullOrEmpty(baseUrl))
                    client.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
                return client;
            });
            services.AddScoped<IAiConnectorClient, AiConnectorClient>();
            services.AddScoped<IAiService, AiService>();

            return services;
        }
    }
}
