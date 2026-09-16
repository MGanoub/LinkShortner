using LinkShortner.Data;
using LinkShortner.Models;
using LinkShortner.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinkShortner.Endpoints;

public static class AuthEndPoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", Register).WithName("Register");
        app.MapPost("/auth/login", Login).WithName("Login");
    }

    public static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] LinkShortenerContext db,
        [FromServices] TokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        if (request.Password.Length < 8)
        {
            return Results.BadRequest(new { error = "Password must be at least 8 characters." });
        }

        var emailExists = await db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return Results.BadRequest(new { error = "An account with this email already exists." });
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.Email));
    }
    
    private static async Task<IResult> Login(
        [FromBody] RegisterRequest request,
        [FromServices] LinkShortenerContext db,
        [FromServices] TokenService tokenService)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var token = tokenService.GenerateToken(user);
        return Results.Ok(new AuthResponse(token, user.Email));
    }
}