using Hangfire;
using Hangfire.PostgreSql;
using JobScheduling.API.Jobs;
using JobScheduling.API.Models;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;

namespace JobScheduling.API.Extensions;

public static class BackgroundJobExtensions
{
    public static IServiceCollection AddBackgroundJob(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTickerQLib();
        services.AddHangFireLib(configuration);

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

    private static IServiceCollection AddHangFireLib(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection")));
        });

        services.AddHangfireServer(options =>
            options.SchedulePollingInterval = TimeSpan.FromSeconds(1));

        return services;
    }

    public static IApplicationBuilder UseBackgroundJob(this WebApplication app)
    {
        app.UseTickerQ();
        app.UseHangfireDashboard();

        //app.Services.UseHangFireJobs();

        return app;
    }

    private static IServiceProvider UseHangFireJobs(this IServiceProvider serviceProvider)
    {
        serviceProvider
            .GetRequiredService<IBackgroundJobClient>()
            .Schedule(() => Console.WriteLine("Hangfire is running!"), TimeSpan.FromSeconds(0));

        serviceProvider
            .GetRequiredService<IBackgroundJobClient>()
            .Schedule<DoSomethingJob>(x =>
                x.HangOnAsync(
                    new SomethingDto { Id = 1, Library = "Hangfire", Message = "Helo from BackgroundJobClient" },
                    default),
                TimeSpan.FromSeconds(5));

        serviceProvider
            .GetRequiredService<IRecurringJobManager>()
            .AddOrUpdate("hangfire-recurring",
                () => Console.WriteLine("Hangfire Recurring Job executed!"),
                Cron.Minutely);

        serviceProvider
            .GetRequiredService<IRecurringJobManager>()
            .AddOrUpdate<DoSomethingJob>(
                "hangfire-recurring",
                job =>
                    job.HangOnAsync(
                        new SomethingDto { Id = 1, Library = "Hangfire", Message = "Helo from RecurringJobManager" },
                        default),
                "0/1 * * * *"
            );

        return serviceProvider;
    }
}
