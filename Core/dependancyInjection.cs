
using Core.DTOS.Validators;
using Core.ServiceContracts;
using Core.Services;
using dBanking.Core.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
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
            return services;
        }
    }
}

