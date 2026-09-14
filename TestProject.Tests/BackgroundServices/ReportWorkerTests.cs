using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TestProject.BackgroundServices;
using TestProject.Options;
using TestProject.Services;
using TestProject.Services.Models;

namespace TestProject.Tests.BackgroundServices;

public sealed class ReportWorkerTests
{
    [Fact]
    public async Task Worker_ContinuesAfterProcessingException()
    {
        var reportService = new SequencedReportService();
        var services = new ServiceCollection();
        services.AddScoped<IReportService>(_ => reportService);
        await using var serviceProvider = services.BuildServiceProvider();
        var worker = new ReportWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ReportWorker>.Instance,
            Microsoft.Extensions.Options.Options.Create(new ReportWorkerOptions
            {
                PollingInterval = TimeSpan.FromMilliseconds(1)
            }));

        await worker.StartAsync(CancellationToken.None);
        await reportService.SecondCall.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);
        worker.Dispose();

        Assert.True(reportService.CallCount >= 2);
    }

    private sealed class SequencedReportService : IReportService
    {
        private readonly TaskCompletionSource _secondCall =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public int CallCount => _callCount;

        public Task SecondCall => _secondCall.Task;

        public Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken)
        {
            var callCount = Interlocked.Increment(ref _callCount);
            if (callCount == 1)
            {
                throw new InvalidOperationException("Первая попытка завершается ошибкой.");
            }

            _secondCall.TrySetResult();
            return Task.CompletedTask;
        }
        
        public Task<Guid> CreateReportJobAsync(
            Guid userId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ReportJobInfo?> GetReportInfoAsync(
            Guid reportJobId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

    }
}
