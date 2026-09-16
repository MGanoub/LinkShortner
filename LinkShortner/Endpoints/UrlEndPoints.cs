using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LinkShortner.Data;
using LinkShortner.Models;
using LinkShortner.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace LinkShortner.Endpoints;

public record ShortenRequest(string Url, int? ExpiresInDays = null);

public static class UrlEndPoints
{
    public static void MapUrlEndPoints(this WebApplication app)
    {
        app.MapPost("/shorten", ShortenUrl)
            .WithName("ShortenUrl")
            .RequireRateLimiting("ShortenPolicy")
            .RequireAuthorization();
        app.MapGet("/{code}", RedirectToUrl).WithName("RedirectToUrl");
        app.MapGet("/api/urls/{code}", GetUrlInfo).WithName("GetUrlInfo");
        app.MapGet("/api/urls/mine", GetMyUrls)
            .WithName("GetMyUrls")
            .RequireAuthorization();
    }

    private static async Task<IResult> ShortenUrl([FromBody] ShortenRequest request,
        [FromServices] LinkShortenerContext db,
        [FromServices] IConfiguration config,
        ClaimsPrincipal user)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return Results.BadRequest(new { error = "A valid absolute URL is required." });
        }

        if (request.ExpiresInDays is not null && request.ExpiresInDays <= 0)
        {
            return Results.BadRequest(new { error = "ExpiresInDays must be a positive number." });
        }
        var userId = int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        string code;
        do
        {
            code = ShortCodeGenerator.Generate();
        } while (await db.ShortenedUrls.AnyAsync(u => u.ShortCode == code));

        var entity = new ShortenedUrl
        {
            ShortCode = code,
            OriginalUrl = request.Url,
            UserId = userId,
            ExpiresAt = request.ExpiresInDays is not null
                ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
                : null
        };
        db.ShortenedUrls.Add(entity);
        await db.SaveChangesAsync();

        var baseUrl = config["BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Results.Problem("Server misconfiguration: BaseUrl is not set.", statusCode: 500);
        }

        var shortUrl = $"{baseUrl}/{code}";
        return Results.Ok(new { shortUrl, code, originalUrl = entity.OriginalUrl, expiresAt = entity.ExpiresAt });
    }

    private static async Task<IResult> RedirectToUrl(string code, [FromServices] LinkShortenerContext db)
    {
        var entity = await db.ShortenedUrls.FirstOrDefaultAsync(u => u.ShortCode == code);
        if (entity is null)
        {
            return Results.NotFound(new { error = "Short link not found." });
        }

        if (entity.ExpiresAt is not null && entity.ExpiresAt < DateTime.UtcNow)
        {
            return Results.StatusCode(StatusCodes.Status410Gone);
        }

        // increment clicks in db directly if concurrent clicks happens
        await db.ShortenedUrls
            .Where(u => u.ShortCode == code)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.ClickCount, u => u.ClickCount + 1));

        return Results.Redirect(entity.OriginalUrl, permanent: false);
    }

    private static async Task<IResult> GetUrlInfo(string code, [FromServices] LinkShortenerContext db)
    {
        var entity = await db.ShortenedUrls.AsNoTracking().FirstOrDefaultAsync(u => u.ShortCode == code);
        if (entity is null)
        {
            return Results.NotFound(new { error = "Short link not found." });
        }

        return Results.Ok(new
        {
            code = entity.ShortCode,
            originalUrl = entity.OriginalUrl,
            createdAt = entity.CreatedAt,
            expiresAt = entity.ExpiresAt,
            clickCount = entity.ClickCount,
            isExpired = entity.ExpiresAt is not null && entity.ExpiresAt < DateTime.UtcNow
        });
    }
    

    private static async Task<IResult> GetMyUrls(
        [FromServices] LinkShortenerContext db,
        [FromServices] IConfiguration config,
        ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var baseUrl = config["BaseUrl"];

        var links = await db.ShortenedUrls
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                code = u.ShortCode,
                shortUrl = $"{baseUrl}/{u.ShortCode}",
                originalUrl = u.OriginalUrl,
                clickCount = u.ClickCount,
                createdAt = u.CreatedAt,
                expiresAt = u.ExpiresAt,
                isExpired = u.ExpiresAt != null && u.ExpiresAt < DateTime.UtcNow
            })
            .ToListAsync();

        return Results.Ok(links);
    }
}