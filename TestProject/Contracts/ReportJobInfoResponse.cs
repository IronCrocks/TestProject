using System.Text.Json.Serialization;

namespace TestProject.Contracts;

public sealed class ReportJobInfoResponse
{
    [JsonPropertyName("query")]
    public Guid ReportJobId { get; init; }

    public int Percent { get; init; }

    public UserStatisticsResult? Result { get; init; }
}
