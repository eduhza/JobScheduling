using JobScheduling.API.Models;

namespace JobScheduling.API.Application.Services.Interfaces;

public interface IDoSomethingService
{
    Task DoSomethingAsync(SomethingDto somethingDto, CancellationToken cancellationToken);
}
