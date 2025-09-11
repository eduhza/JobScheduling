using BenchmarkDotNet.Attributes;
using Hangfire;
using Hangfire.PostgreSql;
using JobScheduling.API;
using JobScheduling.API.Infrastructure.Jobs.DoSomething;
using JobScheduling.API.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
//using TickerQ.Dashboard.DependencyInjection;
//using TickerQ.DependencyInjection;
//using TickerQ.EntityFrameworkCore.DependencyInjection;
//using TickerQ.Utilities;
//using TickerQ.Utilities.Interfaces.Managers;
//using TickerQ.Utilities.Models.Ticker;

namespace JobScheduling.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(runtimeMoniker: BenchmarkDotNet.Jobs.RuntimeMoniker.Net90)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
[MarkdownExporterAttribute.GitHub]
public class JobEnqueuingBenchmarks
{
    private IBackgroundJobClient? _hangfireClient;
    //private ITimeTickerManager<TimeTicker>? _tickerQClient;
    private WebApplication? _app;

    [Params(1, 100, 1000)]
    public int JobCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var builder = WebApplication.CreateBuilder([]);

        builder.Services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));

        var connectionString = "User ID=admin;Password=123123;Host=localhost;Port=5432;Database=jobschedulingdb_benchmarks;Pooling=true;Include Error Detail=true;";
        builder.Services.AddDbContext<DbContext>(options =>
            options.UseNpgsql(connectionString));

        // TickerQ configuration
        //builder.Services.AddTickerQ(options =>
        //{
        //    options.SetMaxConcurrency(Environment.ProcessorCount);
        //    options.UpdateMissedJobCheckDelay(TimeSpan.FromSeconds(5));
        //    options.AddOperationalStore<MyDbContext>(dbOptions =>
        //    {
        //        dbOptions.UseModelCustomizerForMigrations();
        //        dbOptions.CancelMissedTickersOnAppStart();
        //    });
        //    options.AddDashboard(config =>
        //    {
        //        config.BasePath = "/tickerq";
        //        config.EnableBasicAuth = false; // Disable for benchmarks
        //    });
        //});

        // Hangfire configuration
        builder.Services.AddHangfire(config =>
        {
            config.UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString));
        });

        builder.Services.AddHangfireServer(options =>
        {
            options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
            //options.WorkerCount = 1; // Limit workers for consistent benchmarking
        });

        _app = builder.Build();
        //_app.UseTickerQ();
        _app.UseHangfireDashboard();

        // Ensure database is created and migrations are applied
        var dbContext = _app.Services.GetRequiredService<API.DbContext>();
        dbContext.Database.EnsureCreated();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Create a new scope for each iteration to avoid state pollution
        _hangfireClient = _app!.Services.GetRequiredService<IBackgroundJobClient>();
        //_tickerQClient = _app!.Services.GetRequiredService<ITimeTickerManager<TimeTicker>>();
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        _hangfireClient = null;
        //_tickerQClient = null;
    }

    //[GlobalCleanup]
    //public void Cleanup()
    //{
    //    if (_serviceProvider is IDisposable disposable)
    //    {
    //        disposable.Dispose();
    //    }
    //}

    [Benchmark(Description = "Hangfire", Baseline = true)]
    public void EnqueueHangfireJobs()
    {
        var executionTime = TimeSpan.FromSeconds(1);

        for (int i = 0; i < JobCount; i++)
        {
            _hangfireClient!.Schedule<HangfireDoSomethingJob>(job => job.HangOnAsync(new SomethingDto
            {
                Id = i,
                Library = "Hangfire",
                Message = $"Message {i} from BackgroundJobClient"
            }, default),
            executionTime);
        }
    }

    //[Benchmark(Description = "TickerQ")]
    //public async Task EnqueueTickerQJobsAsync()
    //{
    //    var executionTime = DateTime.UtcNow.AddSeconds(1);
    //    var retryIntervals = new[] { 1, 3, 10 };

    //    for (int i = 0; i < JobCount; i++)
    //    {
    //        await _tickerQClient!.AddAsync(new TimeTicker
    //        {
    //            Request = TickerHelper.CreateTickerRequest(new TickerQSomethingDto
    //            {
    //                Id = i,
    //                Library = "TickerQ",
    //                Message = $"Message {i} from TimeTicker."
    //            }),
    //            ExecutionTime = executionTime,
    //            Function = nameof(DoSomethingJob.HangOnAsync),
    //            Description = $"Timed job {i} from TickerQ.",
    //            Retries = 3,
    //            RetryIntervals = retryIntervals
    //        }, default);
    //    }
    //}
}
