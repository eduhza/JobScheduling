using JobScheduling.API.Models;

namespace JobScheduling.API.Services;

public class DoSomethingService(ILogger<DoSomethingService> logger) : IDoSomethingService
{
    public async Task DoSomethingAsync(SomethingDto somethingDto, CancellationToken cancellationToken)
    {
        logger.LogInformation("[{Library}] ID: {Id}. Timestamp: {Timestamp}",
            somethingDto.Library, somethingDto.Id, DateTime.UtcNow);

        Console.WriteLine($"***** {somethingDto.Message} *****");
        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);

        logger.LogInformation("[{Library}] ID: {Id} processamento concluído.",
            somethingDto.Library, somethingDto.Id);
    }
}
