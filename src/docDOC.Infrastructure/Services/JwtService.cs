using docDOC.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace docDOC.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(int userId, string email, string role, string userType, string jti)
    {
        var settings = _configuration.GetSection("JwtSettings");
        var secret = settings["Secret"] ?? throw new InvalidOperationException("JWT Secret is missing.");
        var issuer = settings["Issuer"];
        var audience = settings["Audience"];
        var expiryMinutes = Convert.ToDouble(settings["ExpiryMinutes"]);

        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim("email", email),
            new Claim("role", role),
            new Claim("userType", userType),
            new Claim("jti", jti)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
