using Microsoft.EntityFrameworkCore;
using TestProject.Contracts;
using TestProject.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/report/user_statistics", (UserStatisticsRequest request) =>
{
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

    return Results.Ok(Guid.NewGuid());
})
.WithName("RequestUserStatisticsReport");

app.MapGet("/report/info", (Guid query) =>
{
    if (query == Guid.Empty)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(query)] = ["Идентификатор запроса обязателен."]
        });
    }

    return Results.Ok(new ReportInfoResponse
    {
        Query = query,
        Percent = 0,
        Result = null
    });
})
.WithName("GetReportInfo");

app.Run();
