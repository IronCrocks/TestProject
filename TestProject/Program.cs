using Microsoft.EntityFrameworkCore;
using TestProject.BackgroundServices;
using TestProject.Data;
using TestProject.Endpoints;
using TestProject.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Строка подключения ConnectionStrings:DefaultConnection не задана.");
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services
    .AddOptions<ReportWorkerOptions>()
    .Bind(builder.Configuration.GetSection(ReportWorkerOptions.SectionName))
    .Validate(
        options => options.DurationMilliseconds > 0,
        "ReportWorker:DurationMilliseconds должен быть больше нуля.")
    .ValidateOnStart();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddHostedService<ReportWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapReportEndpoints();

app.Run();
