using Hangfire;
using Hangfire.PostgreSql;
using JobScheduling.API.Application.Jobs.DoSomething;
using JobScheduling.API.Database;
using JobScheduling.API.Infrastructure.Jobs.DoSomething;
using JobScheduling.API.Models;
using Microsoft.EntityFrameworkCore;
using TickerQ.Dashboard.DependencyInjection;
using TickerQ.DependencyInjection;
using TickerQ.EntityFrameworkCore.DependencyInjection;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;

namespace JobScheduling.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabases(configuration);

        services.AddBackgroundJob(configuration);
        return services;
    }

    private static IServiceCollection AddBackgroundJob(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTickerQLib();
        services.AddHangfireLib(configuration);

        return services;
    }

    private static IServiceCollection AddTickerQLib(this IServiceCollection services)
    {
        services.AddTickerQ(options =>
        {
            //options.SetExceptionHandler<MyExceptionHandlerClass>();
            options.SetMaxConcurrency(Environment.ProcessorCount);
            options.UpdateMissedJobCheckDelay(TimeSpan.FromSeconds(5));
            options.AddOperationalStore<JobDbContext>(efOpt =>
            {
                efOpt.UseModelCustomizerForMigrations();
                efOpt.CancelMissedTickersOnAppStart();
            });
            //options.SetInstanceIdentifier("MyAppTickerQInstance");

            options.AddDashboard(config =>
            {
                config.BasePath = "/tickerq";
                config.EnableBasicAuth = true;
            });
        });

        services.AddTransient<IDoSomethingJob, TickerQDoSomethingJob>();

        return services;
    }

    private static IServiceCollection AddHangfireLib(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IDoSomethingJob, HangfireDoSomethingJob>();

        services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(configuration.GetConnectionString("TaskDbConnection")));
        });
        services.AddHangfireServer(options =>
        {
            options.Queues = ["critical", "default", "mail"]; // Prioridade = ordem alfabética
            options.WorkerCount = Environment.ProcessorCount * 5; // Valor default = Environment.ProcessorCount * 5
            options.SchedulePollingInterval = TimeSpan.FromSeconds(1); // Valor default = 15 segundos
        });

        return services;
    }

    public static IApplicationBuilder UseBackgroundJob(this WebApplication app)
    {
        app.UseTickerQ();
        app.UseHangfireDashboard();

        // Example on how to create jobs when the application starts
        //app.Services.UseHangfireJobs();
        //using (var scope = app.Services.CreateScope())
        //{
        //    await scope.ServiceProvider.UseTickerQJobs();//.ConfigureAwait(false).GetAwaiter().GetResult();
        //}

        return app;
    }

    private static IServiceProvider UseHangfireJobs(this IServiceProvider serviceProvider)
    {
        var backgroundJobClient = serviceProvider.GetRequiredService<IBackgroundJobClient>();
        var recurringJobClient = serviceProvider.GetRequiredService<IRecurringJobManager>();

        // Example on how to enqueue a job imediately
        backgroundJobClient.Enqueue(() => Console.WriteLine("Auto Created HangfireJob"));

        // Example on how to schedule a job to run after a delay
        backgroundJobClient
            .Schedule<HangfireDoSomethingJob>(x =>
                x.HangOnAsync(
                    new SomethingDto { Id = 1, Library = "Hangfire", Message = "Helo from BackgroundJobClient" },
                    default),
                TimeSpan.FromSeconds(5));

        // Example on how to create or update a recurring lambda job
        recurringJobClient.AddOrUpdate("console-log",
                () => Console.WriteLine("Hangfire Recurring Job executed!"),
                Cron.Minutely);

        // Example on how to create or update a recurring job for a specific job class
        recurringJobClient
            .AddOrUpdate<HangfireDoSomethingJob>(
                "hangfire-recurring",
                job =>
                    job.HangOnAsync(
                        new SomethingDto { Id = 1, Library = "Hangfire", Message = "Helo from RecurringJobManager" },
                        default),
                "0/1 * * * *"
            );

        return serviceProvider;
    }

    private static async Task<IServiceProvider> UseTickerQJobs(this IServiceProvider serviceProvider)
    {
        var timeTickerManager = serviceProvider.GetRequiredService<ITimeTickerManager<TimeTicker>>();
        var cronTickerManager = serviceProvider.GetRequiredService<ICronTickerManager<CronTicker>>();

        // Example on how to enqueue a job after a delay
        var timeTickerResult = await timeTickerManager.AddAsync(new TimeTicker
        {
            Request = TickerQ.Utilities.TickerHelper.CreateTickerRequest(new SomethingDto
            {
                Id = 1,
                Library = "TickerQ",
                Message = "Helo from TimeTickerManager"
            }),
            ExecutionTime = DateTime.UtcNow.AddSeconds(1),
            Function = nameof(TickerQDoSomethingJob.HangOnAsync),
            Description = "Auto created TimeJob",
            Retries = 3,
            RetryIntervals = [1, 3, 10]
        });

        // Example on how to create a cron job
        var cronTickerResult = await cronTickerManager.AddAsync(new CronTicker
        {
            Request = TickerQ.Utilities.TickerHelper.CreateTickerRequest(new SomethingDto
            {
                Id = 1,
                Library = "TickerQ",
                Message = "Helo from CronTickerManager"
            }),
            Expression = "0/1 * * * *", // Every minute
            Function = nameof(TickerQDoSomethingJob.HangOnAsync),
            Description = "Auto Created CronJob",
            Retries = 3,
            RetryIntervals = [1, 3, 10]
        });

        // Example of a simple console log job
        await cronTickerManager.AddAsync(new CronTicker
        {
            Request = TickerQ.Utilities.TickerHelper.CreateTickerRequest("ConsoleLogJob"),
            Expression = "0/1 * * * *", // Every minute
            Function = "Log",
            Description = "Console Log CronJob",
        });

        return serviceProvider;
    }
}
