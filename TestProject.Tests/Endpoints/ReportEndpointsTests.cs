using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using TestProject.Contracts;
using TestProject.Data.Entities;
using TestProject.Tests.Infrastructure;

namespace TestProject.Tests.Endpoints;

public sealed class ReportEndpointsTests : IAsyncLifetime
{
    private readonly TestApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ReportEndpointsTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public Task InitializeAsync() => _factory.EnsureDatabaseCreatedAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RequestUserStatisticsReport_ReturnsIdAndSavesJob()
    {
        var userId = Guid.NewGuid();
        var request = new UserStatisticsRequest
        {
            UserId = userId,
            From = new DateOnly(2026, 1, 1),
            To = new DateOnly(2026, 1, 31)
        };

        var response = await _client.PostAsJsonAsync("/report/user_statistics", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reportJobId = await response.Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, reportJobId);

        await _factory.ExecuteDbContextAsync(async dbContext =>
        {
            var reportJob = await dbContext.ReportJobs.SingleAsync(job => job.Id == reportJobId);
            Assert.Equal(userId, reportJob.UserId);
            Assert.Equal(ReportJobStatus.Pending, reportJob.Status);
        });
    }

    public static TheoryData<object> InvalidRequests => new()
    {
        new
        {
            userId = Guid.Empty,
            from = "2026-01-01",
            to = "2026-01-31"
        },
        new
        {
            userId = Guid.NewGuid(),
            from = (string?)null,
            to = "2026-01-31"
        },
        new
        {
            userId = Guid.NewGuid(),
            from = "2026-01-01",
            to = (string?)null
        },
        new
        {
            userId = Guid.NewGuid(),
            from = "2026-02-01",
            to = "2026-01-31"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task RequestUserStatisticsReport_ReturnsBadRequest_ForInvalidRequest(object request)
    {
        var response = await _client.PostAsJsonAsync("/report/user_statistics", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestUserStatisticsReport_ReturnsBadRequest_ForInvalidJson()
    {
        using var content = new StringContent("{ invalid", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/report/user_statistics", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReportInfo_ReturnsBadRequest_ForEmptyId()
    {
        var response = await _client.GetAsync($"/report/info?query={Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReportInfo_ReturnsNotFound_ForUnknownJob()
    {
        var response = await _client.GetAsync($"/report/info?query={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReportInfo_ReturnsPendingResponseWithoutResult()
    {
        var reportJob = CreateReportJob(
            _factory.TimeProvider.GetUtcNow().UtcDateTime,
            ReportJobStatus.Pending,
            null);
        await SaveReportJobAsync(reportJob);

        var response = await _client.GetAsync($"/report/info?query={reportJob.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(reportJob.Id, json.RootElement.GetProperty("query").GetGuid());
        Assert.Equal(0, json.RootElement.GetProperty("percent").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("result").ValueKind);
    }

    [Fact]
    public async Task GetReportInfo_ReturnsCompletedResultWithExpectedJsonNames()
    {
        var reportJob = CreateReportJob(
            _factory.TimeProvider.GetUtcNow().UtcDateTime.AddMinutes(-2),
            ReportJobStatus.Completed,
            10);
        await SaveReportJobAsync(reportJob);

        var response = await _client.GetAsync($"/report/info?query={reportJob.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(100, json.RootElement.GetProperty("percent").GetInt32());
        var result = json.RootElement.GetProperty("result");
        Assert.Equal(reportJob.UserId, result.GetProperty("user_id").GetGuid());
        Assert.Equal(10, result.GetProperty("count_sign_in").GetInt32());
    }

    [Fact]
    public async Task UnhandledException_ReturnsProblemDetailsWithoutExceptionMessage()
    {
        using var factory = new TestApplicationFactory(useThrowingReportService: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await factory.EnsureDatabaseCreatedAsync();
        var request = new UserStatisticsRequest
        {
            UserId = Guid.NewGuid(),
            From = new DateOnly(2026, 1, 1),
            To = new DateOnly(2026, 1, 31)
        };

        var response = await client.PostAsJsonAsync("/report/user_statistics", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("Внутренняя тестовая ошибка", body);
    }

    private Task SaveReportJobAsync(ReportJob reportJob)
    {
        return _factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.ReportJobs.Add(reportJob);
            await dbContext.SaveChangesAsync();
        });
    }

    private static ReportJob CreateReportJob(
        DateTime createdAt,
        ReportJobStatus status,
        int? countSignIn)
    {
        return new ReportJob
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            From = new DateTime(2026, 1, 1),
            To = new DateTime(2026, 1, 31),
            CreatedAt = createdAt,
            Status = status,
            CountSignIn = countSignIn
        };
    }
}
