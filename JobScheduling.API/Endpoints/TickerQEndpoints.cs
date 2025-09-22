using JobScheduling.API.Application.Services.Interfaces;
using JobScheduling.API.Infrastructure.Jobs.DoSomething;
using JobScheduling.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TickerQ.Utilities;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;

namespace JobScheduling.API.Endpoints;

public static class TickerQEndpoints
{
    public static void MapTickerQEndpoints(this WebApplication app)
    {
        app.MapPost("/tickerq/timejob", CreateTickerQTimeJob)
            .WithOpenApi()
            .Produces(200);

        app.MapPost("/tickerq/timejob/{count}", CreateMultipleTickerQTimeJob)
            .WithOpenApi()
            .Produces(200);

        app.MapGet("/tickerq/timejob/{jobId}", GetTickerQTimeJob)
            .WithOpenApi()
            .WithName("TickerQJobDetails")
            .Produces(200);

        app.MapPost("/tickerq/cronjob", CreateTickerQCronJob)
            .WithOpenApi()
            .Produces(200);

        app.MapPost("/tickerq/teste/{id}", Test)
            .WithOpenApi()
            .Produces(200);
    }

    #region TickerQ samples
    public static async Task<IResult> CreateTickerQTimeJob(
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        int id = 1)
    {
        var result = await timeTickerManager.AddAsync(new TimeTicker
        {
            Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ")),
            ExecutionTime = DateTime.UtcNow.AddSeconds(1),
            Function = nameof(TickerQDoSomethingJob.HangOnAsync),
            Description = $"Timed job {id} from TickerQ.",
            Retries = 3,
            RetryIntervals = [20, 60, 100] // set in seconds
        });

        var jobId = result?.Result?.Id;

        return Results.Ok(jobId);
        //return Results.AcceptedAtRoute("TickerQJobDetails", new { jobId }, jobId);
    }

    public static async Task<IResult> CreateMultipleTickerQTimeJob(
        [FromRoute] int count,
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager)
    {
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            await CreateTickerQTimeJob(timeTickerManager, i + 1);
        }
        watch.Stop();
        Console.WriteLine($"TickerQ: {watch.ElapsedMilliseconds}");

        return Results.Ok($"Enqueued {count} jobs. Monitor the dashboards.");
    }

    public static IResult GetTickerQTimeJob(
        [FromRoute] string jobId,
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("There is no way to seek for a job.");
    }

    public static async Task<IResult> CreateTickerQCronJob(
        [FromQuery] string cronExpression,
        [FromServices] ICronTickerManager<CronTicker> cronTickerManager)
    {
        var id = Random.Shared.Next(1, 1000);
        await cronTickerManager.AddAsync(new CronTicker
        {
            Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ")),
            Expression = cronExpression,
            Function = nameof(TickerQDoSomethingJob.HangOnAsync),
            Description = $"Cron job {id} from TickerQ",
            Retries = 3,
            RetryIntervals = [1, 3, 10]
        });

        return Results.Ok($"Created cronjob {id}. Monitor the dashboards.");
    }
    #endregion

    #region Tests
    public static async Task<IResult> Test(
        [FromRoute] int id,
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        return id switch
        {
            1 => await Test1e2(timeTickerManager, jobMetricsService),
            2 => Results.BadRequest("TickerQ do not support pod without server"),
            3 => await Test3(timeTickerManager, jobMetricsService),
            4 => await Test4(timeTickerManager, jobMetricsService),
            _ => Results.BadRequest("Invalid test id. Use 1, 3, or 4.")
        };
    }

    // Test 1: Enqueue 1,000,000 jobs. Same POD consuming.
    // Test 2: 2 consumers
    public static async Task<IResult> Test1e2(
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var totalJobs = 1_000_000;
        jobMetricsService.StartInsertion();
        for (var i = 0; i < totalJobs; i++)
        {
            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"TickerQ Normal {i}")),
                ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                Function = nameof(TickerQDoSomethingJob.HangOnAsync),
                Description = $"Timed job {i} from TickerQ.",
                Retries = 3,
                RetryIntervals = [20, 60, 100] // set in seconds
            });
            jobMetricsService.IncrementInsertion();
        }
        jobMetricsService.FinishInsertion();
        stopwatch.Stop();
        try
        {
            var result = jobMetricsService.Snapshot();
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} - {ex.InnerException?.Message}");
            return Results.Ok(stopwatch.ElapsedMilliseconds);
        }
    }

    // Test 3: Enqueue 1,000,000 jobs with "TaskPriority.Normal" and 1,000 jobs with "TaskPriority.High",
    // with "TaskPriority.High" jobs interspersed every 1,000 "TaskPriority.Normal" jobs.
    public static async Task<IResult> Test3(
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var totalDefault = 1_000_000;
        var totalCritical = 1_000;

        jobMetricsService.StartInsertion();
        for (var i = 0; i < totalDefault; i++)
        {
            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"TickerQ Normal {i}")),
                ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                Function = nameof(TickerQDoSomethingJob.HangOnAsync),
                Description = $"Timed job {i} from TickerQ.",
                Retries = 3,
                RetryIntervals = [20, 60, 100] // set in seconds
            });
            jobMetricsService.IncrementInsertion();

            if (i % 1_000 == 0 && totalCritical-- > 0)
            {
                await timeTickerManager.AddAsync(new TimeTicker
                {
                    Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"TickerQ High {i}")),
                    ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                    Function = nameof(TickerQDoSomethingJob.HangOnHighPriorityAsync),
                    Description = $"Timed job {i} from TickerQ.",
                    Retries = 3,
                    RetryIntervals = [20, 60, 100] // set in seconds
                });
                jobMetricsService.IncrementInsertion();
            }
        }
        jobMetricsService.FinishInsertion();

        var result = jobMetricsService.Snapshot();

        return Results.Ok(result);
    }

    // Test 4: Enqueue 1,000,000 jobs with "TaskPriority.Normal" and "TaskPriority.High", one after the other.
    public static async Task<IResult> Test4(
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var totalJobs = 500_000;
        jobMetricsService.StartInsertion();
        for (var i = 0; i < totalJobs; i++)
        {
            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ")),
                ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                Function = nameof(TickerQDoSomethingJob.HangOnAsync),
                Description = $"Timed job {i} from TickerQ.",
                Retries = 3,
                RetryIntervals = [20, 60, 100] // set in seconds
            });
            jobMetricsService.IncrementInsertion();

            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ")),
                ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                Function = nameof(TickerQDoSomethingJob.HangOnHighPriorityAsync),
                Description = $"Timed job {i} from TickerQ.",
                Retries = 3,
                RetryIntervals = [20, 60, 100] // set in seconds
            });
            jobMetricsService.IncrementInsertion();
        }
        jobMetricsService.FinishInsertion();
        var result = jobMetricsService.Snapshot();
        return Results.Ok(result);
    }
    #endregion
}
