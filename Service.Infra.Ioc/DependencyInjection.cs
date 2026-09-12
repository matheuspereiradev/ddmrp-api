using Service.Application.Interfaces;
using Service.Application.Services;
using Service.Domain.Account;
using Service.Domain.Interfaces;
using Service.Infra.Data.Context;
using Service.Infra.Data.Identity;
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
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                   b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            });

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
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

            services.AddScoped<IAuthenticate, AuthenticateProvider>();
            services.AddScoped<IAuthenticateService, AuthenticateService>();

            return services;
        }
    }
}
