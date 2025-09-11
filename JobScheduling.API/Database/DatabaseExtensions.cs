using Microsoft.EntityFrameworkCore;

namespace JobScheduling.API.Database;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddDbContext<JobDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TaskDbConnection")));

        return services;
    }
}
