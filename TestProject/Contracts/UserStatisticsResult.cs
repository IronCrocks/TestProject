namespace TestProject.Contracts;

public sealed class UserStatisticsResult
{
    public Guid UserId { get; init; }

    public int Count { get; init; }
}
