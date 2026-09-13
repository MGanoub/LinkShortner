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
        app.MapPost("/shorten", ShortenUrl).WithName("ShortenUrl");
        app.MapGet("/{code}", RedirectToUrl).WithName("RedirectToUrl");
        app.MapGet("/api/urls/{code}", GetUrlInfo).WithName("GetUrlInfo");
        app.MapGet("/api/urls/all", GetAllUrlsInfo).WithName("GetAllUrlsInfo");
    }

    private static async Task<IResult> ShortenUrl([FromBody] ShortenRequest request,
        [FromServices] LinkShortenerContext db,
        [FromServices] IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        {
            return Results.BadRequest(new { error = "A valid absolute URL is required." });
        }
        if (request.ExpiresInDays is not null && request.ExpiresInDays <= 0)
        {
            return Results.BadRequest(new { error = "ExpiresInDays must be a positive number." });
        }

        string code;
        do
        {
            code = ShortCodeGenerator.Generate();
        } while (await db.ShortenedUrls.AnyAsync(u => u.ShortCode == code));

        var entity = new ShortenedUrl
        {
            ShortCode = code,
            OriginalUrl = request.Url,
            ExpiresAt = request.ExpiresInDays is not null
                ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
                : null
        };
        db.ShortenedUrls.Add(entity);
        await db.SaveChangesAsync();

        var baseUrl = config["BaseUrl"] ?? "https://localhost:7162";
        var shortUrl = $"{baseUrl}/{code}";
        return Results.Ok(new { shortUrl, code, originalUrl = entity.OriginalUrl, expiresAt = entity.ExpiresAt});
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
    
    private static async Task<IResult> GetAllUrlsInfo([FromServices] LinkShortenerContext db)
    {
        var entities  = await db.ShortenedUrls.AsNoTracking().ToListAsync();
        if (entities.Count == 0)
        {
            return Results.NotFound(new { error = "No short links found." });
        }

        return Results.Ok(entities.Select(entity => new
        {
            code = entity.ShortCode,
            originalUrl = entity.OriginalUrl,
            createdAt = entity.CreatedAt,
            expiresAt = entity.ExpiresAt,
            clickCount = entity.ClickCount,
            isExpired = entity.ExpiresAt is not null && entity.ExpiresAt < DateTime.UtcNow
        }));
    }
}