namespace TestProject.Options;

public sealed class ReportWorkerOptions
{
    public const string SectionName = "ReportWorker";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);
}
