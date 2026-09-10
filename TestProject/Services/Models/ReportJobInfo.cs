namespace TestProject.Services.Models;

public sealed class ReportJobInfo
{
    public Guid ReportJobId { get; init; }

    public int Percent { get; init; }

    public UserStatisticsReportResult? Result { get; init; }
}
