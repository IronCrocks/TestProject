namespace TestProject.Data.Entities;

public sealed class ReportJob
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public DateTime CreatedAt { get; set; }

    public ReportJobStatus Status { get; set; } = ReportJobStatus.Pending;

    public int? CountSignIn { get; set; }
}
