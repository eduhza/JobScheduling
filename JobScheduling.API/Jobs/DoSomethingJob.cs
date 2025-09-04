using JobScheduling.API.Models;
using JobScheduling.API.Services;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace JobScheduling.API.Jobs;

public class DoSomethingJob(
    IDoSomethingService doSomethingService,
    ILogger<DoSomethingJob> logger)
{
    private readonly ILogger<DoSomethingJob> _logger = logger;

    [TickerFunction(functionName: nameof(HangOnAsync))]
    public async Task HangOnAsync(TickerFunctionContext<SomethingDto> tickerContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{JobName}] Job iniciado. Id: {Id}.",
           nameof(HangOnAsync), tickerContext.Request.Id);

        await doSomethingService.DoSomethingAsync(tickerContext.Request, cancellationToken);

        _logger.LogInformation("[{JobName}] Job finalizado. Id: {Id}.",
            nameof(HangOnAsync), tickerContext.Request.Id);
    }
}
