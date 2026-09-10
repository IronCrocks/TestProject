using Microsoft.Extensions.Options;
using TestProject.Options;
using TestProject.Services;

namespace TestProject.BackgroundServices;

public sealed class ReportWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ReportWorker> logger,
    IOptions<ReportWorkerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CompleteExpiredReportJobsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Не удалось обработать просроченные отчёты.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
        await reportService.CompleteExpiredReportJobsAsync(cancellationToken);
    }
}
