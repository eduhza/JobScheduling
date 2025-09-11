using JobScheduling.API.Models;

namespace JobScheduling.API.Application.Services;

public class DoSomethingService(ILogger<DoSomethingService> logger) : IDoSomethingService
{
    public async Task DoSomethingAsync(SomethingDto somethingDto, CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Library}] ID: {Id}. Message: {Message}",
            somethingDto.Library, somethingDto.Id, somethingDto.Message);
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
    }
}
