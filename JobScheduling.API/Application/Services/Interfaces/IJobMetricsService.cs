namespace JobScheduling.API.Application.Services.Interfaces;

public interface IJobMetricsService
{
    void StartInsertion();
    void FinishInsertion();
    void IncrementInsertion();
    void RegisterExecution(Guid id, DateTime scheduled, DateTime start, DateTime end);
    MetricsSnapshot Snapshot();
}
