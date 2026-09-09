using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TestProject.BackgroundServices;
using TestProject.Contracts;
using TestProject.Data;
using TestProject.Data.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<ReportWorkerOptions>(
    builder.Configuration.GetSection(ReportWorkerOptions.SectionName));
builder.Services.AddHostedService<ReportWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/report/user_statistics", async (
    UserStatisticsRequest request,
    ApplicationDbContext dbContext,
    CancellationToken cancellationToken) =>
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
})
.WithName("RequestUserStatisticsReport");

app.MapGet("/report/info", async (
    Guid query,
    ApplicationDbContext dbContext,
    IOptions<ReportWorkerOptions> options,
    CancellationToken cancellationToken) =>
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
})
.WithName("GetReportInfo");

app.Run();
