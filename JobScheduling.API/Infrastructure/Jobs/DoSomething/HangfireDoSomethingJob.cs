using JobScheduling.API.Application.Jobs.DoSomething;
using JobScheduling.API.Application.Services.Interfaces;
using JobScheduling.API.Models;

namespace JobScheduling.API.Infrastructure.Jobs.DoSomething;

public class HangfireDoSomethingJob(
    IDoSomethingService service,
    IJobMetricsService jobMetricsService
    ) : IDoSomethingJob
{
    //[Queue("mail")]
    public async Task HangOnAsync(SomethingDto dto, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        await service.DoSomethingAsync(dto, cancellationToken);
        var endTime = DateTime.UtcNow;
        jobMetricsService.RegisterExecution(dto.Id, dto.CreatedAt, startTime, endTime);
    }
}
