using JobScheduling.API.Models;

namespace JobScheduling.API.Services;

public interface IDoSomethingService
{
    Task DoSomethingAsync(SomethingDto somethingDto, CancellationToken cancellationToken);
}
