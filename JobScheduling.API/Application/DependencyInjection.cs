using JobScheduling.API.Application.Services;
using JobScheduling.API.Application.Services.Interfaces;

namespace JobScheduling.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDoSomethingService, DoSomethingService>();

        return services;
    }
}
