using System.Text.Json.Serialization;

namespace TestProject.Contracts;

public sealed class UserStatisticsResult
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("count_sign_in")]
    public int CountSignIn { get; init; }
}
