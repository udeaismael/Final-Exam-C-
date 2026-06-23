using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RentalAPI.Domain.Entities;

namespace RentalAPI.Infrastructure.Services;

public class JwtService(IConfiguration config)
{
    private readonly string _key       = config["Jwt:Key"]!;
    private readonly string _issuer    = config["Jwt:Issuer"]!;
    private readonly string _audience  = config["Jwt:Audience"]!;
    private readonly int _expiresMin   = int.Parse(config["Jwt:ExpiresInMinutes"] ?? "60");

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Role,           user.Role),
            new Claim("firstName",               user.FirstName),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_expiresMin),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
