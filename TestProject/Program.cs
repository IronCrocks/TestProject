using Microsoft.EntityFrameworkCore;
using TestProject.BackgroundServices;
using TestProject.Data;
using TestProject.Endpoints;

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

app.MapReportEndpoints();

app.Run();
