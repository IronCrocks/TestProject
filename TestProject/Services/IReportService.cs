using TestProject.Contracts;

namespace TestProject.Services;

public interface IReportService
{
    Task<Guid> CreateReportJobAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<ReportInfoResponse?> GetReportInfoAsync(Guid query, CancellationToken cancellationToken);

    Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken);
}
