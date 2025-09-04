using Hangfire;
using JobScheduling.API.Jobs;
using JobScheduling.API.Models;
using Microsoft.AspNetCore.Mvc;
using TickerQ.Utilities;
using TickerQ.Utilities.Interfaces.Managers;
using TickerQ.Utilities.Models.Ticker;

namespace JobScheduling.API.Endpoints;

public static class SchedulingApi
{
    public static void MapSchedulingEndpoints(this WebApplication app)
    {
        app.MapPost("/enqueue-timejob/{count}", HandleEnqueueTimeJob)
            .WithName("enqueue-timeticker")
            .WithOpenApi()
            .Produces(200);

        app.MapPost("/enqueue-cronjob", HandleEnqueueCronJob)
            .WithName("enqueue-cronticker")
            .WithOpenApi()
            .Produces(200);
    }

    public static async Task<IResult> HandleEnqueueTimeJob(
        [FromRoute] int count,
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        [FromServices] IBackgroundJobClient hangfireClient,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < count; i++)
        {
            hangfireClient.Enqueue<DoSomethingJob>(job => job.HangOnAsync(new SomethingDto
            {
                Id = i,
                Library = "Hangfire",
                Message = $"Message {i} from BackgroundJobClient"
            }, cancellationToken));

            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(new SomethingDto
                {
                    Id = i,
                    Library = "TickerQ",
                    Message = $"Message {i} from TimeTicker."
                }),
                ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                Function = nameof(DoSomethingJob.HangOnAsync),
                Description = $"Timed job {i} from TickerQ.",
                Retries = 3,
                RetryIntervals = [1, 3, 10]
            }, cancellationToken);
        }

        return Results.Ok($"Enqueued {count} jobs. Monitor the dashboards.");
    }

    public static async Task<IResult> HandleEnqueueCronJob(
        [FromQuery] string cronExpression,
        [FromServices] ICronTickerManager<CronTicker> cronTickerManager,
        [FromServices] IRecurringJobManager recurringJobManager,
        CancellationToken cancellationToken)
    {
        var id = Random.Shared.Next(1, 1000);
        recurringJobManager.AddOrUpdate<DoSomethingJob>(
            "hangfire-recurring",
            job => job.HangOnAsync(new SomethingDto
            {
                Id = id,
                Library = "Hangfire",
                Message = $"Message {id} from RecurringJobManager"
            }, cancellationToken),
            cronExpression);

        await cronTickerManager.AddAsync(new CronTicker
        {
            Request = TickerHelper.CreateTickerRequest(new SomethingDto
            {
                Id = id,
                Library = "TickerQ",
                Message = $"Message {id} from CronTicker."
            }),
            Expression = cronExpression,
            Function = nameof(DoSomethingJob.HangOnAsync),
            Description = $"Cron job {id} from TickerQ",
            Retries = 3,
            RetryIntervals = [1, 3, 10]
        }, cancellationToken);

        return Results.Ok($"Enqueued {count} jobs. Monitor the dashboards.");
    }
}