using JobScheduling.API.Models;
using JobScheduling.API.Services;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace JobScheduling.API.Jobs;

public class DoSomethingJob(IDoSomethingService doSomethingService)
{
    // TickerQ
    [TickerFunction(functionName: nameof(HangOnAsync))]
    public async Task HangOnAsync(TickerFunctionContext<SomethingDto> tickerContext, CancellationToken cancellationToken)
    {
        await doSomethingService.DoSomethingAsync(tickerContext.Request, cancellationToken);
    }

    // Hangfire
    public async Task HangOnAsync(SomethingDto somethingDto, CancellationToken cancellationToken)
    {
        await doSomethingService.DoSomethingAsync(somethingDto, cancellationToken);
    }
}
