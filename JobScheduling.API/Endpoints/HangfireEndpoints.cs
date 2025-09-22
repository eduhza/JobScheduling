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

        app.MapPost("/hangfire/teste/{id}", Test)
            .WithOpenApi()
            .Produces(200);
    }

    #region Hangfire samples
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
    #endregion

    #region Tests

    public static async Task<IResult> Test(
        [FromRoute] int id,
        [FromServices] IBackgroundJobClient backgroundJobClient,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        return id switch
        {
            1 => await Test1e2(backgroundJobClient, jobMetricsService, "emails"),
            2 => await Test1e2(backgroundJobClient, jobMetricsService, "default"),
            3 => await Test3(backgroundJobClient, jobMetricsService),
            4 => await Test4(backgroundJobClient, jobMetricsService),
            _ => Results.BadRequest("Invalid test id. Use 1, 3, or 4.")
        };
    }

    // Test 1: Enqueue 1,000,000 jobs in "emails" queue. Same POD consuming.
    // Test 2: Enqueue 1,000,000 jobs in "default" queue. Another POD consuming.
    public static async Task<IResult> Test1e2(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        [FromServices] IJobMetricsService jobMetricsService,
        string queue)
    {
        var totalJobs = 1_000_000;
        jobMetricsService.StartInsertion();

        await Parallel.ForEachAsync(Enumerable.Range(0, totalJobs), new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (i, ct) =>
        {
            backgroundJobClient.Create<HangfireDoSomethingJob>(queue,
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire {queue} - {i}"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();
        });
        //for (int i = 0; i < totalJobs; i++)
        //{
        //    backgroundJobClient.Create<HangfireDoSomethingJob>(queue,
        //        job => job.HangOnAsync(
        //            new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire {queue} - {i}"),
        //            CancellationToken.None),
        //        state: new EnqueuedState());
        //    jobMetricsService.IncrementInsertion();
        //}
        jobMetricsService.FinishInsertion();
        var result = jobMetricsService.Snapshot();
        return Results.Ok(result);
    }

    // Test 3: Enqueue 1,000,000 jobs in "default" queue and 1,000 jobs in "critical" queue,
    // with "critical" jobs interspersed every 1,000 "default" jobs.
    public static async Task<IResult> Test3(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var totalDefault = 1_000_000;
        var totalCritical = 1_000;

        jobMetricsService.StartInsertion();
        await Parallel.ForEachAsync(Enumerable.Range(0, totalDefault), new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (i, ct) =>
        {
            backgroundJobClient.Create<HangfireDoSomethingJob>("default",
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire Test3 Default - {i}"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();

            if (i % 1_000 == 0 && totalCritical-- > 0)
            {
                backgroundJobClient.Create<HangfireDoSomethingJob>("critical",
                    job => job.HangOnAsync(
                        new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire Test3 Critical - {i}"),
                        CancellationToken.None),
                    state: new EnqueuedState());
                jobMetricsService.IncrementInsertion();
            }
        });

        //for (int i = 0; i < totalDefault; i++)
        //{
        //    backgroundJobClient.Create<HangfireDoSomethingJob>("default",
        //        job => job.HangOnAsync(
        //            new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire Test3 Default - {i}"),
        //            CancellationToken.None),
        //        state: new EnqueuedState());
        //    jobMetricsService.IncrementInsertion();

        //    if (i % 1_000 == 0 && totalCritical-- > 0)
        //    {
        //        backgroundJobClient.Create<HangfireDoSomethingJob>("critical",
        //            job => job.HangOnAsync(
        //                new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire Test3 Critical - {i}"),
        //                CancellationToken.None),
        //            state: new EnqueuedState());
        //        jobMetricsService.IncrementInsertion();
        //    }
        //}
        jobMetricsService.FinishInsertion();

        var result = jobMetricsService.Snapshot();

        return Results.Ok(result);
    }

    // Test 4: Enqueue 500,000 jobs in "default" and "critical" queues, one after the other.
    public static async Task<IResult> Test4(
        [FromServices] IBackgroundJobClient backgroundJobClient,
        [FromServices] IJobMetricsService jobMetricsService)
    {
        var totalJobs = 500_000;
        jobMetricsService.StartInsertion();
        await Parallel.ForEachAsync(Enumerable.Range(0, totalJobs), new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (i, ct) =>
        {
            backgroundJobClient.Create<HangfireDoSomethingJob>("default",
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire Test3 Default - {i}"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();

            backgroundJobClient.Create<HangfireDoSomethingJob>("critical",
                job => job.HangOnAsync(
                    new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, $"Hangfire Test3 Critical - {i}"),
                    CancellationToken.None),
                state: new EnqueuedState());
            jobMetricsService.IncrementInsertion();
        });
        jobMetricsService.FinishInsertion();
        var result = jobMetricsService.Snapshot();
        return Results.Ok(result);
    }
    #endregion
}