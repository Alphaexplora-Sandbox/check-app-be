using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CheckAppBackend.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CheckAppBackend.Infrastructure.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var secret = _configuration["Jwt:Key"] ?? Convert.ToBase64String(Encoding.UTF8.GetBytes("ec_mock_key_32chars_long_1234567"));
        var issuer = _configuration["Jwt:Issuer"] ?? "ecommerce-mock";
        var audience = _configuration["Jwt:Audience"] ?? "ecommerce-mock-clients";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
