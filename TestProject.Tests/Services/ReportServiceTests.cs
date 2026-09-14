using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestProject.Data.Entities;
using TestProject.Options;
using TestProject.Services;
using TestProject.Tests.Infrastructure;

namespace TestProject.Tests.Services;

public sealed class ReportServiceTests
{
    private static readonly DateTimeOffset InitialTime =
        new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateReportJobAsync_SavesPendingJobAndReturnsItsId()
    {
        await using var database = new SqliteTestDatabase();
        var timeProvider = new ManualTimeProvider(InitialTime);
        var service = CreateService(database, timeProvider);
        var userId = Guid.NewGuid();
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);

        var reportJobId = await service.CreateReportJobAsync(userId, from, to, CancellationToken.None);

        var reportJob = await database.Context.ReportJobs.SingleAsync();
        Assert.NotEqual(Guid.Empty, reportJobId);
        Assert.Equal(reportJobId, reportJob.Id);
        Assert.Equal(userId, reportJob.UserId);
        Assert.Equal(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), reportJob.From);
        Assert.Equal(to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), reportJob.To);
        Assert.Equal(InitialTime.UtcDateTime, reportJob.CreatedAt);
        Assert.Equal(ReportJobStatus.Pending, reportJob.Status);
        Assert.Null(reportJob.CountSignIn);
    }

    [Fact]
    public async Task GetReportInfoAsync_ReturnsNull_WhenJobDoesNotExist()
    {
        await using var database = new SqliteTestDatabase();
        var service = CreateService(database, new ManualTimeProvider(InitialTime));

        var result = await service.GetReportInfoAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetReportInfoAsync_ReturnsHalfProgress_ForPendingJobAtHalfDuration()
    {
        await using var database = new SqliteTestDatabase();
        var timeProvider = new ManualTimeProvider(InitialTime);
        var reportJob = await AddReportJobAsync(database, InitialTime.UtcDateTime);
        var service = CreateService(database, timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(30));

        var result = await service.GetReportInfoAsync(reportJob.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(50, result.Percent);
        Assert.Null(result.Result);
        Assert.Equal(ReportJobStatus.Pending, reportJob.Status);
    }

    [Fact]
    public async Task GetReportInfoAsync_DoesNotReturnNegativeProgress_WhenCreatedAtIsInFuture()
    {
        await using var database = new SqliteTestDatabase();
        var reportJob = await AddReportJobAsync(
            database,
            InitialTime.AddMinutes(1).UtcDateTime);
        var service = CreateService(database, new ManualTimeProvider(InitialTime));

        var result = await service.GetReportInfoAsync(reportJob.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.Percent);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task GetReportInfoAsync_CompletesExpiredJobAndReturnsResult()
    {
        await using var database = new SqliteTestDatabase();
        var timeProvider = new ManualTimeProvider(InitialTime);
        var reportJob = await AddReportJobAsync(database, InitialTime.UtcDateTime);
        var service = CreateService(database, timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(60));

        var result = await service.GetReportInfoAsync(reportJob.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.Percent);
        Assert.NotNull(result.Result);
        Assert.Equal(reportJob.UserId, result.Result.UserId);
        Assert.Equal(10, result.Result.CountSignIn);

        database.Context.ChangeTracker.Clear();
        var savedJob = await database.Context.ReportJobs.SingleAsync();
        Assert.Equal(ReportJobStatus.Completed, savedJob.Status);
        Assert.Equal(10, savedJob.CountSignIn);
    }

    [Fact]
    public async Task GetReportInfoAsync_DoesNotChangeCompletedJob()
    {
        await using var database = new SqliteTestDatabase();
        var reportJob = await AddReportJobAsync(
            database,
            InitialTime.AddMinutes(-5).UtcDateTime,
            ReportJobStatus.Completed,
            7);
        var service = CreateService(database, new ManualTimeProvider(InitialTime));

        var result = await service.GetReportInfoAsync(reportJob.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.Percent);
        Assert.Equal(7, result.Result?.CountSignIn);
        Assert.Equal(7, reportJob.CountSignIn);
    }

    [Fact]
    public async Task CompleteExpiredReportJobsAsync_CompletesOnlyExpiredPendingJobs()
    {
        await using var database = new SqliteTestDatabase();
        var expiredJob = await AddReportJobAsync(
            database,
            InitialTime.AddMinutes(-2).UtcDateTime);
        var freshJob = await AddReportJobAsync(
            database,
            InitialTime.AddSeconds(-30).UtcDateTime);
        var completedJob = await AddReportJobAsync(
            database,
            InitialTime.AddMinutes(-2).UtcDateTime,
            ReportJobStatus.Completed,
            4);
        var service = CreateService(database, new ManualTimeProvider(InitialTime));

        await service.CompleteExpiredReportJobsAsync(CancellationToken.None);

        Assert.Equal(ReportJobStatus.Completed, expiredJob.Status);
        Assert.Equal(10, expiredJob.CountSignIn);
        Assert.Equal(ReportJobStatus.Pending, freshJob.Status);
        Assert.Null(freshJob.CountSignIn);
        Assert.Equal(ReportJobStatus.Completed, completedJob.Status);
        Assert.Equal(4, completedJob.CountSignIn);
    }

    private static ReportService CreateService(
        SqliteTestDatabase database,
        TimeProvider timeProvider)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ReportProcessingOptions
        {
            DurationMilliseconds = 60_000
        });

        return new ReportService(database.Context, options, timeProvider);
    }

    private static async Task<ReportJob> AddReportJobAsync(
        SqliteTestDatabase database,
        DateTime createdAt,
        ReportJobStatus status = ReportJobStatus.Pending,
        int? countSignIn = null)
    {
        var reportJob = new ReportJob
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            From = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = createdAt,
            Status = status,
            CountSignIn = countSignIn
        };

        database.Context.ReportJobs.Add(reportJob);
        await database.Context.SaveChangesAsync();

        return reportJob;
    }
}
