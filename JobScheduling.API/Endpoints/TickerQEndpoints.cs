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
            .Produces(200);

        app.MapPost("/tickerq/cronjob", CreateTickerQCronJob)
            .WithName("tickerq-cronjob")
            .WithOpenApi()
            .Produces(200);
    }

    public static async Task<IResult> CreateTickerQTimeJob(
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await timeTickerManager.AddAsync(new TimeTicker
        {
            Request = TickerHelper.CreateTickerRequest(new SomethingDto
            {
                Id = id,
                Library = "TickerQ",
                Message = $"Message {id} from TimeTicker."
            }),
            ExecutionTime = DateTime.UtcNow.AddSeconds(10),
            Function = nameof(TickerQDoSomethingJob.HangOnAsync),
            Description = $"Timed job {id} from TickerQ.",
            Retries = 3,
            RetryIntervals = [1, 3, 10]
        }, cancellationToken);

        var jobId = result.Result.Id;

        return Results.AcceptedAtRoute("JobDetails", new { jobId }, jobId);
    }

    public static async Task<IResult> CreateMultipleTickerQTimeJob(
        [FromRoute] int count,
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            await CreateTickerQTimeJob(timeTickerManager, i + 1, cancellationToken);
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
        [FromServices] ICronTickerManager<CronTicker> cronTickerManager,
        CancellationToken cancellationToken)
    {
        var id = Random.Shared.Next(1, 1000);
        await cronTickerManager.AddAsync(new CronTicker
        {
            Request = TickerHelper.CreateTickerRequest(new SomethingDto
            {
                Id = id,
                Library = "TickerQ",
                Message = $"Message {id} from CronTicker."
            }),
            Expression = cronExpression,
            Function = nameof(TickerQDoSomethingJob.HangOnAsync),
            Description = $"Cron job {id} from TickerQ",
            Retries = 3,
            RetryIntervals = [1, 3, 10]
        }, cancellationToken);

        return Results.Ok($"Created cronjob {id}. Monitor the dashboards.");
    }


}
