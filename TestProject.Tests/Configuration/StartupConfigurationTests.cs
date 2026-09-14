using TestProject.Tests.Infrastructure;

namespace TestProject.Tests.Configuration;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void ApplicationFailsToStart_WhenConnectionStringIsEmpty()
    {
        using var factory = CreateFactory("ConnectionStrings:DefaultConnection", " ");

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(
            "ConnectionStrings:DefaultConnection не задана",
            exception.ToString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void ApplicationFailsToStart_WhenReportDurationIsNotPositive(string value)
    {
        using var factory = CreateFactory("ReportProcessing:DurationMilliseconds", value);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(
            "ReportProcessing:DurationMilliseconds должен быть больше нуля",
            exception.ToString());
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    public void ApplicationFailsToStart_WhenPollingIntervalIsNotPositive(string value)
    {
        using var factory = CreateFactory("ReportWorker:PollingInterval", value);

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(
            "ReportWorker:PollingInterval должен быть больше нуля",
            exception.ToString());
    }

    private static TestApplicationFactory CreateFactory(string key, string value)
    {
        return new TestApplicationFactory(configurationOverrides: new Dictionary<string, string?>
        {
            [key] = value
        });
    }
}
