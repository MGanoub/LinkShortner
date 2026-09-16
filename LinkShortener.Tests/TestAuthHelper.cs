using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LinkShortener.Tests;

public static class TestAuthHelper
{
    public static async Task AuthenticateAsync(this HttpClient client)
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password = "password123!"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}