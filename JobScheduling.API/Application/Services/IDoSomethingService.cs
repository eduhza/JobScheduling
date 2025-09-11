using JobScheduling.API.Models;

namespace JobScheduling.API.Application.Services;

public interface IDoSomethingService
{
    Task DoSomethingAsync(SomethingDto somethingDto, CancellationToken cancellationToken);
}
