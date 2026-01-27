using Core.ServiceContracts;
using Core.Services;
using Core.RepositoryContracts;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class dependancyInjection
{
    // Extension method to add core services to the IServiceCollection
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {// Register core services here
        // e.g., services.AddTransient<IMyService, MyService>();
        // Services
        services.AddTransient<ICustomerService, CustomerService>();

        return services;
    }
}

