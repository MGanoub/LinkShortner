using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinkShortener.Tests.Fixtures;
using LinkShortner.Data;
using LinkShortner.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Tests;

public class RedirectEndPointTests: IClassFixture<LinkShortenerApiFixture>
{
    private readonly LinkShortenerApiFixture _fixture;

    public RedirectEndPointTests(LinkShortenerApiFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> CreateShortLink(HttpClient client, string url = "https://example.com")
    {
        var response = await client.PostAsJsonAsync("/shorten", new {url});
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("code").ToString();
    }

    [Fact]
    public async Task Redirect_WithValidCode_Returns302ToOriginalUrl()
    {
        var setupClient = _fixture.CreateClient();
        await setupClient.AuthenticateAsync();
        var code = await CreateShortLink(setupClient, "https://example.com/valid-code-test");
        var redirectClient = _fixture.CreateClientNoRedirect();
        var response = await redirectClient.GetAsync($"{code}");
        
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("https://example.com/valid-code-test", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Redirect_WithUnknownCode_Returns404()
    {
        var client = _fixture.CreateClient();
        var response = await client.GetAsync("/thisCodeDoesNotExist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Redirect_WithExpiredCode_Returns410()
    {
        const string code = "expTest";

        using (var scope = _fixture.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LinkShortenerContext>();
            
            var user = new User
            {
                Email = $"expiry-test-{Guid.NewGuid():N}@example.com",
                PasswordHash = "not-used-in-this-test"
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            
            db.ShortenedUrls.Add(new ShortenedUrl
            {
                ShortCode = code,
                OriginalUrl = "https://example.com/already-expired",
                ExpiresAt = DateTime.UtcNow.AddDays(-1), // already in the past
                UserId = user.Id
            });

            await db.SaveChangesAsync();
        }

        var redirectClient = _fixture.CreateClientNoRedirect();
        var response = await redirectClient.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode); // 410
    }
}