namespace TestProject.BackgroundServices;

public sealed class ReportWorkerOptions
{
    public const string SectionName = "ReportWorker";

    public int DurationMilliseconds { get; set; } = 60000;
}
