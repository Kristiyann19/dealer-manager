using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DealerManager.WebAPI.Extensions;
using DealerManager.WebAPI.ExceptionHandling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DealerManagerDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("MainDbConnectionString")
    ));

builder.Services.AddControllers();
builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CandidateExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
