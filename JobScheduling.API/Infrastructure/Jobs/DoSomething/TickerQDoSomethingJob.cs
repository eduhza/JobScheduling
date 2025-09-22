using JobScheduling.API.Application.Jobs.DoSomething;
using JobScheduling.API.Application.Services.Interfaces;
using JobScheduling.API.Models;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace JobScheduling.API.Infrastructure.Jobs.DoSomething;

public class TickerQDoSomethingJob(
    IDoSomethingService service,
    IJobMetricsService jobMetricsService) : IDoSomethingJob
{
    [TickerFunction(nameof(HangOnAsync), TickerQ.Utilities.Enums.TickerTaskPriority.Normal)]
    public async Task HangOnAsync(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        await service.DoSomethingAsync(ctx.Request, ct);
        var endTime = DateTime.UtcNow;
        jobMetricsService.RegisterExecution(ctx.Request.Id, ctx.Request.CreatedAt, startTime, endTime);
    }

    [TickerFunction(nameof(HangOnHighPriorityAsync), TickerQ.Utilities.Enums.TickerTaskPriority.High)]
    public async Task HangOnHighPriorityAsync(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        await service.DoSomethingAsync(ctx.Request, ct);
        var endTime = DateTime.UtcNow;
        jobMetricsService.RegisterExecution(ctx.Request.Id, ctx.Request.CreatedAt, startTime, endTime);
    }

    [TickerFunction(nameof(ConsoleLog), TickerQ.Utilities.Enums.TickerTaskPriority.Normal)]
    public void ConsoleLog(TickerFunctionContext<string> ctx)
    {
        Console.WriteLine(ctx.Request);
    }

    [TickerFunction(nameof(EmptyConsoleLog), TickerQ.Utilities.Enums.TickerTaskPriority.Normal)]
    public void EmptyConsoleLog()
    {
        Console.WriteLine("PARAMETERLESS");
    }

    //[TickerFunction(nameof(CronConsoleLog), "* * * * *")]
    //public void CronConsoleLog()
    //{
    //    Console.WriteLine("This is a cronjob.");
    //}

    [TickerFunction(nameof(CronJob1), "0/1 * * * *")]
    public async Task CronJob1(CancellationToken ct)
    {
        var somethingDto = new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ CronJob1");
        await service.DoSomethingAsync(somethingDto, ct);
    }

    [TickerFunction(nameof(CronJob2), "0/2 * * * *")]
    public async Task CronJob2(CancellationToken ct)
    {
        var somethingDto = new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ CronJob2");
        await service.DoSomethingAsync(somethingDto, ct);
    }

    [TickerFunction(nameof(CronJob3), "0/2 * * * *")]
    public async Task CronJob3(CancellationToken ct)
    {
        var somethingDto = new SomethingDto(Guid.NewGuid(), DateTime.UtcNow, "TickerQ CronJob3");
        await service.DoSomethingAsync(somethingDto, ct);
    }

    public async Task HangOnAsync(SomethingDto dto, CancellationToken ct)
    {
        await service.DoSomethingAsync(dto, ct);
    }
}
