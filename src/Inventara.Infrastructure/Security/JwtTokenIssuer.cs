using System.Security.Claims;
using System.Text;
using Inventara.Application.Auth;
using Inventara.Application.Security;
using Inventara.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Inventara.Infrastructure.Security;

public sealed class JwtTokenIssuer(JwtSettings settings) : IJwtTokenIssuer
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    public IssuedToken Issue(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(TokenLifetime);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("security_stamp", user.SecurityStamp.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim("name", user.FullName),
                new Claim("department", user.Department),
                new Claim("phone", user.Phone)
            })
        };
        var value = new JsonWebTokenHandler().CreateToken(descriptor);

        return new IssuedToken(value, new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }
}
