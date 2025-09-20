using Hangfire;
using Hangfire.States;
using JobScheduling.API.Application.Services.Interfaces;
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
            .WithName("HangfireJobDetails")
            .Produces(200);

        app.MapPost("/hangfire/cronjob", CreateHangfireCronJob)
            .WithOpenApi()
            .Produces(200);
    }

    public static IResult CreateHangFireJob(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        CancellationToken cancellationToken = default)
    {
        var jobId = backgroundJobClient.Create<HangfireDoSomethingJob>(
            //queue: "mail",
            job => job.HangOnAsync(
                new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "Hangfire"),
                CancellationToken.None),
            state: new EnqueuedState());

        //backgroundJobClient.ContinueJobWith(jobId,
        //    () => Console.WriteLine($"Job {jobId} has been processed."));

        return Results.AcceptedAtRoute("HangfireJobDetails", new { jobId }, jobId);
    }

    public static IResult CreateMultipleHangfireJobs(
        [FromRoute] int count,
        [FromServices] IBackgroundJobClient hangfireClient,
        CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            CreateHangFireJob(hangfireClient, cancellationToken);
        }
        watch.Stop();

        return Results.Ok($"Enqueued {count} jobs in {watch.Elapsed}. Monitor the dashboards.");
    }

    public static IResult GetHangfireTimeJob(
        [FromRoute] string jobId,
        CancellationToken cancellationToken)
    {
        var state = JobStorage.Current.GetConnection().GetJobData(jobId);
        if (state == null)
            return Results.NotFound($"Job {jobId} not found.");

        var jobDetais = JobStorage.Current.GetMonitoringApi().JobDetails(jobId);
        var lastJob = jobDetais.History.OrderByDescending(h => h.CreatedAt).First();

        return Results.Ok(lastJob);
    }

    public static IResult CreateHangfireCronJob(
        [FromQuery] string cronExpression,
        [FromServices] IRecurringJobManager recurringJobManager,
        CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        recurringJobManager.AddOrUpdate<HangfireDoSomethingJob>(
            "hangfire-recurring",
            job => job.HangOnAsync(
                new SomethingDto(id, DateTime.UtcNow, $"Hangfire Cronjob {id}"),
                cancellationToken),
            cronExpression);

        return Results.Ok($"Created cronjob {id}. Monitor the dashboards.");
    }

    // Test 3: Enqueue 1,000,000 jobs in "default" queue and 1,000 jobs in "critical" queue,
    // with "critical" jobs interspersed every 1,000 "default" jobs.
    public static IResult Test3(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var totalDefault = 1_000_000;
        var totalCritical = 1_000;

        jobMetricsService.StartInsertion();
        for (int i = 0; i < totalDefault; i++)
        {
            backgroundJobClient.Create<HangfireDoSomethingJob>("default",
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "Hangfire Test3 Default"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();

            if (i % 1_000 == 0 && totalCritical-- > 0)
            {
                backgroundJobClient.Create<HangfireDoSomethingJob>("critical",
                    job => job.HangOnAsync(
                        new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "Hangfire Test3 Critical"),
                        CancellationToken.None),
                    state: new EnqueuedState());
                jobMetricsService.IncrementInsertion();
            }
        }
        jobMetricsService.FinishInsertion();

        var result = jobMetricsService.Snapshot();

        return Results.Ok(result);
    }

    // Test 4: Enqueue 1,000,000 jobs in "default" and "critical" queues, one after the other.
    public static IResult Test4(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var totalJobs = 1_000_000;
        jobMetricsService.StartInsertion();
        for (int i = 0; i < totalJobs; i++)
        {
            backgroundJobClient.Create<HangfireDoSomethingJob>("default",
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "Hangfire Test4 Default"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();
            backgroundJobClient.Create<HangfireDoSomethingJob>("critical",
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "Hangfire Test4 Critical"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();


            jobMetricsService.IncrementInsertion();



        }
        jobMetricsService.FinishInsertion();
        var result = jobMetricsService.Snapshot();
        return Results.Ok(result);
    }

}