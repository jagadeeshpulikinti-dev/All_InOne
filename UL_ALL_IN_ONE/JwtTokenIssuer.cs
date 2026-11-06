using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UL_ALL_IN_ONE;

public sealed class JwtTokenIssuer(IConfiguration configuration) : ITokenIssuer
{
    public AuthResponse Issue(string userName, string role)
    {
        var key = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        if (Encoding.UTF8.GetByteCount(key) < 32) throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        var expires = DateTime.UtcNow.AddMinutes(30);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], new[] { new Claim(ClaimTypes.Name, userName), new Claim(JwtRegisteredClaimNames.Sub, userName), new Claim(ClaimTypes.Role, role) }, expires: expires, signingCredentials: credentials);
        return new(new JwtSecurityTokenHandler().WriteToken(token), expires, userName, role);
    }
}
