using JobScheduling.API.Application.Services.Interfaces;
using JobScheduling.API.Models;

namespace JobScheduling.API.Application.Services;

public class DoSomethingService(ILogger<DoSomethingService> logger) : IDoSomethingService
{
    public async Task DoSomethingAsync(SomethingDto somethingDto, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        logger.LogInformation("[{Message}] Enviando email ID {Id}...", somethingDto.Message, somethingDto.Id.ToString()[0..5]);
        await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken);
        logger.LogInformation("[{Message}] ID: {Id}... Criado: {CreatedAt} - Executado {Now}",
            somethingDto.Message, somethingDto.Id.ToString()[0..5], somethingDto.CreatedAt, now);
    }
}
