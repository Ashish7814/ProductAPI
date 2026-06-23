using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Interfaces;
using Product.Infrastructure.Data;
using Product.Infrastructure.Identity;
using Product.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Product.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // ── Database ─────────────────────────────────────────────────────────
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                )
            );

            // ── Repositories & Unit of Work ───────────────────────────────────────
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ── Identity / JWT ────────────────────────────────────────────────────
            services.AddScoped<ITokenService, TokenService>();

            // ── External Services ─────────────────────────────────────────────────
            services.AddScoped<IEmailNotificationService, EmailNotificationService>();
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

            // ── Caching ───────────────────────────────────────────────────────────
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();

            return services;
        }
    }
}
