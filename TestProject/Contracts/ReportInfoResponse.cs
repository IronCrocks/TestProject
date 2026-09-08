namespace TestProject.Contracts;

public sealed class ReportInfoResponse
{
    public Guid Query { get; init; }

    public int Percent { get; init; }

    public UserStatisticsResult? Result { get; init; }
}
