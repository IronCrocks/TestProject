using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestProject.Data;
using TestProject.Data.Entities;

namespace TestProject.BackgroundServices;

public sealed class ReportWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ReportWorkerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        do
        {
            await CompleteExpiredReportJobsAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pendingReportJobs = await dbContext.ReportJobs
            .Where(reportJob => reportJob.Status == ReportJobStatus.Pending)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var duration = TimeSpan.FromMilliseconds(options.Value.DurationMilliseconds);
        var hasChanges = false;

        foreach (var reportJob in pendingReportJobs)
        {
            if (now - reportJob.CreatedAt < duration)
            {
                continue;
            }

            reportJob.Status = ReportJobStatus.Completed;
            reportJob.CountSignIn ??= 10;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
