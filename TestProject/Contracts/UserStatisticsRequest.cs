namespace TestProject.Contracts;

public sealed class UserStatisticsRequest
{
    public Guid UserId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}
