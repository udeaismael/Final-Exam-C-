using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentalAPI.Domain.Entities;
using RentalAPI.Infrastructure.Data;
using RentalAPI.Infrastructure.Services;

namespace RentalAPI.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, JwtService jwt) : ControllerBase
{
    // ── POST /api/auth/register ──────────────────────────────────────────────
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict("Email already registered.");

        var user = new User
        {
            Email        = req.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            Role         = req.Role is "Owner" or "Admin" ? req.Role : "User"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new { user.Id, user.Email, user.Role });
    }

    // ── POST /api/auth/login ─────────────────────────────────────────────────
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower());
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials.");

        if (!user.IsActive)
            return Forbid();

        var accessToken   = jwt.GenerateAccessToken(user);
        var refreshToken  = JwtService.GenerateRefreshToken();
        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new TokenResponse(accessToken, refreshToken));
    }

    // ── POST /api/auth/refresh ───────────────────────────────────────────────
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == req.RefreshToken);
        if (user is null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized("Invalid or expired refresh token.");

        var accessToken  = jwt.GenerateAccessToken(user);
        var refreshToken = JwtService.GenerateRefreshToken();
        user.RefreshToken       = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await db.SaveChangesAsync();

        return Ok(new TokenResponse(accessToken, refreshToken));
    }
}

public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? Role);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken);
