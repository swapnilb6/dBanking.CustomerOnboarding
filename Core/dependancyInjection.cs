using Core.DTOS.Validators;
using Core.ServiceContracts;

using Core.Services;
using dBanking.Core.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
namespace dBanking.Core
{
    public static class dependancyInjection
    {
        // Extension method to add core services to the IServiceCollection
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // Register core services here
            // e.g., services.AddTransient<IMyService, MyService>();
            // Services
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddValidatorsFromAssemblyContaining<CustomerUpdateRequestDtoValidator>();
            services.AddScoped<IKycCaseService, KycCaseService>();

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = "op1-redis:6379"; // Update with your Redis configuration
                options.InstanceName = "dBankingCache_";
            });

            return services;
        }
    }
}

