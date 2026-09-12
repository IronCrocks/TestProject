namespace TestProject.Services.Models;

public sealed class UserStatisticsReportResult
{
    public Guid UserId { get; init; }

    public int? CountSignIn { get; init; }
}
