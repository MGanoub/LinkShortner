using System.Net;
using System.Net.Http.Json;
using LinkShortener.Tests.Fixtures;

namespace LinkShortener.Tests;

public class RateLimitingTests : IClassFixture<RateLimitedApiFixture>
{
    private readonly RateLimitedApiFixture  _fixture;
    
    public RateLimitingTests(RateLimitedApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Shorten_ExceedingLimit_Returns429()
    {
        var client = _fixture.CreateClient();
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 4; i++)
        {
            lastResponse = await client.PostAsJsonAsync("/shorten", new
            {
                url = $"https://example.com/rate-limit-test-{i}"
            });
            if (i < 3)
            {
                Assert.Equal(HttpStatusCode.OK, lastResponse.StatusCode);
            }
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }
}