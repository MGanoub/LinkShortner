using LinkShortner.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace LinkShortener.Tests.Fixtures;

public class LinkShortenerApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("linkshortner_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        // apply migrations for fresh container
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LinkShortenerContext>();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:ShortenPermitLimit"] = "10000",
                ["RateLimiting:ShortenWindowSeconds"] = "60",
                ["BaseUrl"] = "https://localhost:5001",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                ["Jwt:Key"] = "SNbN+Qwckd2YAftWmz6410w/8GWfUW7BWxCBahEn9ag=",
                ["Jwt:Issuer"] = "LinkShortener",
                ["Jwt:Audience"] = "LinkShortenerUsers",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });
        
        builder.ConfigureServices(services =>
        {
            // remove if any DbContext registered by real app & point to one in test container
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LinkShortenerContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<LinkShortenerContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public HttpClient CreateClientNoRedirect()
    {
        return CreateDefaultClient(new NoRedirectHandler());
    }

    private class NoRedirectHandler : DelegatingHandler
    {
        public NoRedirectHandler() : base(new HttpClientHandler { AllowAutoRedirect = false })
        {
        }
    }
    // Explicit interface implementation satisfies IAsyncLifetime.DisposeAsync() (returns Task)
    Task IAsyncLifetime.DisposeAsync() => _postgres.DisposeAsync().AsTask();
    
    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}