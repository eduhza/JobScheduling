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
        app.MapPost("/enqueue-timeticker/{count}", HandleEnqueueTimeTicker)
            .WithName("enqueue-timeticker")
            .WithOpenApi()
            .Produces(200);

        app.MapPost("/enqueue-cronticker/{count}", HandleEnqueueCronTicker)
            .WithName("enqueue-cronticker")
            .WithOpenApi()
            .Produces(200);
    }

    public static async Task<IResult> HandleEnqueueTimeTicker(
        [FromRoute] int count,
        [FromServices] ITimeTickerManager<TimeTicker> timeTickerManager,
        //[FromServices] IBackgroundJobClient hangfireClient,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < count; i++)
        {
            var somethingDto = new SomethingDto { Id = i, Library = "TickerQ", Message = $"Message {i} from TimeTicker." };
            //    hangfireClient.Enqueue<EmailSenderJob>(job => job.SendEmailAsync(i, "Hangfire"));

            await timeTickerManager.AddAsync(new TimeTicker
            {
                Request = TickerHelper.CreateTickerRequest(somethingDto),
                ExecutionTime = DateTime.UtcNow.AddSeconds(10),
                Function = nameof(DoSomethingJob.HangOnAsync),
                Description = $"Envio de e-mail ID: {i} via TickerQ",
                Retries = 3,
                RetryIntervals = [1, 3, 10]
            }, cancellationToken);
        }

        return Results.Ok($"Enqueued {count} jobs. Monitor the dashboards.");
    }

    public static async Task<IResult> HandleEnqueueCronTicker(
        [FromRoute] int count,
        [FromServices] ICronTickerManager<CronTicker> cronTickerManager,
        //[FromServices] IBackgroundJobClient hangfireClient,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < count; i++)
        {
            var somethingDto = new SomethingDto { Id = i, Library = "TickerQ", Message = $"Message {i} from CronTicker." };
            //    recurringJobManager.AddOrUpdate<EmailSenderJob>(
            //        "hangfire-recurring",
            //        job => job.SendEmailAsync(999, "Hangfire-Recurring"),
            //        Cron.Minutely);

            await cronTickerManager.AddAsync(new CronTicker
            {
                Request = TickerHelper.CreateTickerRequest(somethingDto),
                Expression = "* * * * *",
                Function = nameof(DoSomethingJob.HangOnAsync),
                Description = $"Envio de e-mail ID: {i} via TickerQ",
                Retries = 3,
                RetryIntervals = [1, 3, 10]
            }, cancellationToken);
        }

        return Results.Ok($"Enqueued {count} jobs. Monitor the dashboards.");
    }
}