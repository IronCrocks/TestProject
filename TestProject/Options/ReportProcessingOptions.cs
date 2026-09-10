namespace TestProject.Options;

public sealed class ReportProcessingOptions
{
    public const string SectionName = "ReportProcessing";

    public int DurationMilliseconds { get; set; } = 60000;
}
