using JobScheduling.API.Application.Jobs.DoSomething;
using JobScheduling.API.Application.Services;
using JobScheduling.API.Models;

namespace JobScheduling.API.Infrastructure.Jobs.DoSomething;

public class HangfireDoSomethingJob(IDoSomethingService service) : IDoSomethingJob
{
    //[Queue("mail")]
    public async Task HangOnAsync(SomethingDto dto, CancellationToken cancellationToken)
    {
        await service.DoSomethingAsync(dto, cancellationToken);
    }
}
