using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LinkShortener.Tests.Fixtures;
using Xunit;

namespace LinkShortener.Tests;

public class LookupEndpointTests : IClassFixture<LinkShortenerApiFixture>
{
    private readonly LinkShortenerApiFixture _fixture;

    public LookupEndpointTests(LinkShortenerApiFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> CreateShortLink(HttpClient client, string url = "https://example.com/lookup-test")
    {
        var response = await client.PostAsJsonAsync("/shorten", new { url });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task Lookup_WithValidCode_ReturnsMetadataWithZeroClicks()
    {
        var client = _fixture.CreateClient();
        await client.AuthenticateAsync();
        var code = await CreateShortLink(client);

        var response = await client.GetAsync($"/api/urls/{code}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("clickCount").GetInt32());
        Assert.Equal("https://example.com/lookup-test", body.GetProperty("originalUrl").GetString());
    }

    [Fact]
    public async Task Lookup_WithUnknownCode_Returns404()
    {
        var client = _fixture.CreateClient();

        var response = await client.GetAsync("/api/urls/doesNotExist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ClickCount_IncrementsAfterRedirectVisit()
    {
        var setupClient = _fixture.CreateClient();
        await setupClient.AuthenticateAsync();
        var code = await CreateShortLink(setupClient, "https://example.com/click-count-test");

        var redirectClient = _fixture.CreateClientNoRedirect();
        await redirectClient.GetAsync($"/{code}"); // triggers the increment

        var lookupResponse = await setupClient.GetAsync($"/api/urls/{code}");
        var body = await lookupResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, body.GetProperty("clickCount").GetInt32());
    }
}