using Core.RepositoryContracts;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;
public static class dependancyInjection
{
    // Extension method to add infrastructure services to the IServiceCollection
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Register infrastructure services here
        // e.g., services.AddTransient<IMyService, MyService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IKycCaseRepository, KycCaseRepository>();
        return services;
    }
}

