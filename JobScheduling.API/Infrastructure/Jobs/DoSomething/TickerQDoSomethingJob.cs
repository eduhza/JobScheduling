using JobScheduling.API.Application.Jobs.DoSomething;
using JobScheduling.API.Application.Services.Interfaces;
using JobScheduling.API.Models;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace JobScheduling.API.Infrastructure.Jobs.DoSomething;

public class TickerQDoSomethingJob(IDoSomethingService service) : IDoSomethingJob
{
    [TickerFunction(nameof(HangOnAsync), TickerQ.Utilities.Enums.TickerTaskPriority.Normal)]
    public async Task HangOnAsync(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        await service.DoSomethingAsync(ctx.Request, ct);
    }

    [TickerFunction(nameof(HangOnHighPriorityAsync), TickerQ.Utilities.Enums.TickerTaskPriority.High)]
    public async Task HangOnHighPriorityAsync(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        await service.DoSomethingAsync(ctx.Request, ct);
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
    public async Task CronJob1(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        await service.DoSomethingAsync(ctx.Request, ct);
    }

    [TickerFunction(nameof(CronJob2), "0/2 * * * *")]
    public async Task CronJob2(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        await service.DoSomethingAsync(ctx.Request, ct);
    }

    [TickerFunction(nameof(CronJob3), "0/2 * * * *")]
    public async Task CronJob3(TickerFunctionContext<SomethingDto> ctx, CancellationToken ct)
    {
        await service.DoSomethingAsync(ctx.Request, ct);
    }

    public async Task HangOnAsync(SomethingDto dto, CancellationToken ct)
    {
        await service.DoSomethingAsync(dto, ct);
    }
}
