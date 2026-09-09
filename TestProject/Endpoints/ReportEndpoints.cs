using TestProject.Contracts;
using TestProject.Services;

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
        IReportService reportService,
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

        var reportJobId = await reportService.CreateReportJobAsync(
            request.UserId,
            request.From.Value,
            request.To.Value,
            cancellationToken);

        return Results.Ok(reportJobId);
    }

    private static async Task<IResult> GetReportInfo(
        Guid query,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        if (query == Guid.Empty)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(query)] = ["Идентификатор запроса обязателен."]
            });
        }

        var reportInfo = await reportService.GetReportInfoAsync(query, cancellationToken);

        return reportInfo is null
            ? Results.NotFound()
            : Results.Ok(reportInfo);
    }
}
