using Hangfire;
using JobScheduling.API.Infrastructure.Jobs.DoSomething;
using JobScheduling.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JobScheduling.API.Endpoints;

public static class HangfireEndpoints
{
    public static void MapHangfireEndpoints(this WebApplication app)
    {
        app.MapPost("/hangfire/timejob", CreateHangFireJob)
            .WithOpenApi()
            .Produces(200);

        app.MapPost("/hangfire/timejob/{count}", CreateMultipleHangfireJobs)
            .WithOpenApi()
            .Produces(200);

        app.MapGet("/hangfire/timejob/{jobId}", GetHangfireTimeJob)
            .WithOpenApi()
            .Produces(200);

        app.MapPost("/hangfire/cronjob", CreateHangfireCronJob)
            .WithOpenApi()
            .Produces(200);
    }

    public static IResult CreateHangFireJob(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var jobId = backgroundJobClient.Schedule<HangfireDoSomethingJob>(
            //queue: "mail",
            job => job.HangOnAsync(new SomethingDto
            {
                Id = id,
                Library = "Hangfire",
                Message = $"Message {id} from BackgroundJobClient"
            }, cancellationToken),
        TimeSpan.FromSeconds(10));

        backgroundJobClient.ContinueJobWith(jobId,
            () => Console.WriteLine($"Job {jobId} has been processed."));

        return Results.AcceptedAtRoute("JobDetails", new { jobId }, jobId);
    }

    public static IResult CreateMultipleHangfireJobs(
        [FromRoute] int count,
        [FromServices] IBackgroundJobClient hangfireClient,
        CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            CreateHangFireJob(hangfireClient, i + 1, cancellationToken);
        }
        watch.Stop();

        return Results.Ok($"Enqueued {count} jobs. Monitor the dashboards.");
    }

    public static IResult GetHangfireTimeJob(
        [FromRoute] string jobId,
        CancellationToken cancellationToken)
    {
        var state = JobStorage.Current.GetConnection().GetJobData(jobId);
        if (state == null)
            return Results.NotFound($"Job {jobId} not found.");

        var jobDetais = JobStorage.Current.GetMonitoringApi().JobDetails(jobId);

        return Results.Ok(state.State);
        return Results.Ok(jobDetais.History.OrderByDescending(h => h.CreatedAt).First().StateName);
    }

    public static IResult CreateHangfireCronJob(
        [FromQuery] string cronExpression,
        [FromServices] IRecurringJobManager recurringJobManager,
        CancellationToken cancellationToken)
    {
        var id = Random.Shared.Next(1, 1000);
        recurringJobManager.AddOrUpdate<HangfireDoSomethingJob>(
            "hangfire-recurring",
            job => job.HangOnAsync(new SomethingDto
            {
                Id = id,
                Library = "Hangfire",
                Message = $"Message {id} from RecurringJobManager"
            }, cancellationToken),
            cronExpression);

        return Results.Ok($"Created cronjob {id}. Monitor the dashboards.");
    }
}