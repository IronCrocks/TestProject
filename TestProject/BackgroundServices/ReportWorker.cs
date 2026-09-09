using TestProject.Services;

namespace TestProject.BackgroundServices;

public sealed class ReportWorker(
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
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
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        await reportService.CompleteExpiredReportJobsAsync(cancellationToken);
    }
}
