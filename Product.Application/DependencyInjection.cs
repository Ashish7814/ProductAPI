
using Product.Application.Interfaces;
using Product.Application.Mapping;
using Product.Application.Services;
using Product.Application.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Product.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
