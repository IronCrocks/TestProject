using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TestProject.BackgroundServices;
using TestProject.Data;
using TestProject.Services;
using TestProject.Services.Models;

namespace TestProject.Tests.Infrastructure;

internal sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly IReadOnlyDictionary<string, string?>? _configurationOverrides;
    private readonly bool _useThrowingReportService;

    public TestApplicationFactory(
        bool useThrowingReportService = false,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _useThrowingReportService = useThrowingReportService;
        _configurationOverrides = configurationOverrides;
        _connection.Open();
    }

    public ManualTimeProvider TimeProvider { get; } = new(
        new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (_configurationOverrides is not null)
        {
            foreach (var (key, value) in _configurationOverrides)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureAppConfiguration(configuration =>
                configuration.AddInMemoryCollection(_configurationOverrides));
        }

        builder.ConfigureServices(services =>
        {
            var workerDescriptor = services.SingleOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(ReportWorker));

            if (workerDescriptor is not null)
            {
                services.Remove(workerDescriptor);
            }

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);

            if (_useThrowingReportService)
            {
                services.RemoveAll<IReportService>();
                services.AddScoped<IReportService, ThrowingReportService>();
            }
        });
    }

    public async Task EnsureDatabaseCreatedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(dbContext);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }

    private sealed class ThrowingReportService : IReportService
    {
        public Task<Guid> CreateReportJobAsync(
            Guid userId,
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Внутренняя тестовая ошибка.");
        }

        public Task<ReportJobInfo?> GetReportInfoAsync(
            Guid reportJobId,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Внутренняя тестовая ошибка.");
        }

        public Task CompleteExpiredReportJobsAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Внутренняя тестовая ошибка.");
        }
    }
}
