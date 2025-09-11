using JobScheduling.API.Models;

namespace JobScheduling.API.Application.Jobs.DoSomething;

public interface IDoSomethingJob
{
    Task HangOnAsync(SomethingDto dto, CancellationToken cancellationToken);
}
