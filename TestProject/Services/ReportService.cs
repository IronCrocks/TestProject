using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestProject.BackgroundServices;
using TestProject.Contracts;
using TestProject.Data;
using TestProject.Data.Entities;

namespace TestProject.Services;

public sealed class ReportService(
    ApplicationDbContext dbContext,
    IOptions<ReportWorkerOptions> options) : IReportService
{
    public async Task<Guid> CreateReportJobAsync(
        Guid userId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var reportJobId = Guid.NewGuid();
        var reportJob = new ReportJob
        {
            Id = reportJobId,
            UserId = userId,
            From = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            To = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            Status = ReportJobStatus.Pending,
            CountSignIn = null
        };

        dbContext.ReportJobs.Add(reportJob);
        await dbContext.SaveChangesAsync(cancellationToken);

        return reportJobId;
    }

    public async Task<ReportInfoResponse?> GetReportInfoAsync(
        Guid query,
        CancellationToken cancellationToken)
    {
        var reportJob = await dbContext.ReportJobs
            .SingleOrDefaultAsync(reportJob => reportJob.Id == query, cancellationToken);

        if (reportJob is null)
        {
            return null;
        }

        if (CompleteReportIfExpired(reportJob))
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var durationMilliseconds = options.Value.DurationMilliseconds;
        var percent = reportJob.Status == ReportJobStatus.Completed
            ? 100
            : (int)Math.Clamp(
                Math.Floor((DateTime.UtcNow - reportJob.CreatedAt).TotalMilliseconds * 100 / durationMilliseconds),
                0,
                100);

        return new ReportInfoResponse
        {
            Query = query,
            Percent = percent,
            Result = percent == 100
                ? new UserStatisticsResult
                {
                    UserId = reportJob.UserId,
                    CountSignIn = reportJob.CountSignIn ?? 10
                }
                : null
        };
    }

    public async Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken)
    {
        var completionCutoff = DateTime.UtcNow
            .AddMilliseconds(-options.Value.DurationMilliseconds);
        var pendingReportJobs = await dbContext.ReportJobs
            .Where(reportJob =>
                reportJob.Status == ReportJobStatus.Pending &&
                reportJob.CreatedAt <= completionCutoff)
            .ToListAsync(cancellationToken);

        var hasChanges = false;

        foreach (var reportJob in pendingReportJobs)
        {
            hasChanges |= CompleteReportIfExpired(reportJob);
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private bool CompleteReportIfExpired(ReportJob reportJob)
    {
        if (reportJob.Status != ReportJobStatus.Pending)
        {
            return false;
        }

        var duration = TimeSpan.FromMilliseconds(options.Value.DurationMilliseconds);
        if (DateTime.UtcNow - reportJob.CreatedAt < duration)
        {
            return false;
        }

        reportJob.Status = ReportJobStatus.Completed;
        reportJob.CountSignIn = 10;

        return true;
    }
}
