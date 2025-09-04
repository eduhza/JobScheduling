using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace JobScheduling.API.Extensions;

public static class BackgroundJobExtensions
{
    public static IServiceCollection AddBackgrounJob(this IServiceCollection services)
    {
        services.AddTickerQLib();
        services.AddHangFireLib();

        return services;
    }

    private static IServiceCollection AddTickerQLib(this IServiceCollection services)
    {
        services.AddTickerQ(options =>
        {
            //options.SetExceptionHandler<MyExceptionHandlerClass>();
            options.SetMaxConcurrency(Environment.ProcessorCount);
            options.UpdateMissedJobCheckDelay(TimeSpan.FromSeconds(5));
            options.AddOperationalStore<MyDbContext>(dbOptions =>
            {
                dbOptions.UseModelCustomizerForMigrations();
                dbOptions.CancelMissedTickersOnAppStart();
            });

            options.AddDashboard(config =>
            {
                config.BasePath = "/tickerq";
                config.EnableBasicAuth = true;
            });
        });

        return services;
    }

    private static IServiceCollection AddHangFireLib(this IServiceCollection services)
    {

        return services;
    }
}
