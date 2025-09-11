using JobScheduling.API.Application.Jobs.DoSomething;
using JobScheduling.API.Application.Services;
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

    [TickerFunction(nameof(ConsoleLog), TickerQ.Utilities.Enums.TickerTaskPriority.Normal)]
    public void ConsoleLog(TickerFunctionContext<string> ctx)
    {
        Console.WriteLine(ctx.Request);
    }

    [TickerFunction(nameof(CronConsoleLog), "* * * * *")]
    public void CronConsoleLog()
    {
        Console.WriteLine("This is a cronjob.");
    }

    public async Task HangOnAsync(SomethingDto dto, CancellationToken ct)
    {
        await service.DoSomethingAsync(dto, ct);
    }
}
