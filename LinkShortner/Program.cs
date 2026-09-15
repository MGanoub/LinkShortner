using System.Threading.RateLimiting;
using LinkShortner.Data;
using LinkShortner.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LinkShortenerContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ShortenPolicy", context =>
    {
        var config = context.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue<int?>("RateLimiting:ShortenPermitLimit") ?? 10;
        var windowSeconds = config.GetValue<int?>("RateLimiting:ShortenWindowSeconds") ?? 60;
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        
        Console.WriteLine($"[RateLimiter] Key={key} PermitLimit={permitLimit}, WindowSeconds={windowSeconds}");

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend"); 
app.UseRateLimiter();
app.MapUrlEndPoints();

app.Run();

public partial class Program { }