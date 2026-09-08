using Microsoft.EntityFrameworkCore;
using TestProject.Data.Entities;

namespace TestProject.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<ReportJob> ReportJobs => Set<ReportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportJob>(entity =>
        {
            entity.HasKey(reportJob => reportJob.Id);

            entity.Property(reportJob => reportJob.Status)
                .HasConversion<string>()
                .IsRequired();

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ReportJobs_CountSignIn_NonNegative",
                "\"CountSignIn\" >= 0"));
        });
    }
}
