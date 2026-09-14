using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LinkShortener.Tests.Fixtures;

namespace LinkShortener.Tests;

public class ShortenEndpointTests : IClassFixture<LinkShortenerApiFixture>
{
    private readonly HttpClient _client;

    public ShortenEndpointTests(LinkShortenerApiFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task shorten_withValidUrl_Returns200AndShortCode()
    {
        var response = await _client.PostAsJsonAsync("/shorten", new {url = "https://example.com"});
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().HaveLength(6);
    }

    [Fact]
    public async Task Shorten_with_InvalidUrl_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/shorten", new {url = "not a url"});
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
    }
    
    [Fact]
    public async Task Shorten_WithPastExpiresAt_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/shorten", new
        {
            url = "https://example.com",
            expiresInDays = -1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}