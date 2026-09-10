using TestProject.Services.Models;

namespace TestProject.Services;

public interface IReportService
{
    Task<Guid> CreateReportJobAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);

    Task<ReportJobInfo?> GetReportInfoAsync(Guid reportJobId, CancellationToken cancellationToken);

    Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken);
}
