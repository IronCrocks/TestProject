using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestProject.BackgroundServices;
using TestProject.Contracts;
using TestProject.Data;
using TestProject.Data.Entities;

namespace TestProject.Endpoints;

public static class ReportEndpoints
{
    public static WebApplication MapReportEndpoints(this WebApplication app)
    {
        var reports = app.MapGroup("/report");

        reports.MapPost("/user_statistics", RequestUserStatisticsReport)
            .WithName("RequestUserStatisticsReport");
        reports.MapGet("/info", GetReportInfo)
            .WithName("GetReportInfo");

        return app;
    }

    private static async Task<IResult> RequestUserStatisticsReport(
        UserStatisticsRequest request,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        
#region Validation

        if (request.UserId == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.UserId)] = ["Идентификатор пользователя обязателен."]
            });
        }

        if (request.From is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.From)] = ["Дата начала периода обязательна."]
            });
        }

        if (request.To is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.To)] = ["Дата окончания периода обязательна."]
            });
        }

        if (request.From > request.To)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.From)] = ["Дата начала периода не может быть позже даты окончания."]
            });
        }

#endregion

        var reportJobId = Guid.NewGuid();
        var reportJob = new ReportJob
        {
            Id = reportJobId,
            UserId = request.UserId,
            From = request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            To = request.To.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            Status = ReportJobStatus.Pending,
            CountSignIn = null
        };

        dbContext.ReportJobs.Add(reportJob);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(reportJobId);
    }

    private static async Task<IResult> GetReportInfo(
        Guid query,
        ApplicationDbContext dbContext,
        IOptions<ReportWorkerOptions> options,
        CancellationToken cancellationToken)
    {
        if (query == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(query)] = ["Идентификатор запроса обязателен."]
            });
        }

        var reportJob = await dbContext.ReportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(reportJob => reportJob.Id == query, cancellationToken);

        if (reportJob is null)
        {
            return Results.NotFound();
        }

        var durationMilliseconds = options.Value.DurationMilliseconds;
        if (durationMilliseconds <= 0)
        {
            return Results.Problem(
                "Значение ReportWorker:DurationMilliseconds должно быть больше нуля.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var percent = reportJob.Status == ReportJobStatus.Completed
            ? 100
            : (int)Math.Clamp(
                Math.Floor((DateTime.UtcNow - reportJob.CreatedAt).TotalMilliseconds * 100 / durationMilliseconds),
                0,
                100);

        return Results.Ok(new ReportInfoResponse
        {
            Query = query,
            Percent = percent,
            Result = percent == 100
                ? new UserStatisticsResult
                {
                    UserId = reportJob.UserId,
                    CountSignIn = reportJob.CountSignIn ?? 10
                }
                : null
        });
    }
}
