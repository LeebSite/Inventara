using System.Text;
using Microsoft.Extensions.Configuration;

namespace Inventara.Infrastructure.Security;

public sealed class JwtSettings
{
    private JwtSettings(string issuer, string audience, string signingKey)
    {
        Issuer = issuer;
        Audience = audience;
        SigningKey = signingKey;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public string SigningKey { get; }

    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var signingKey = configuration["Jwt:SigningKey"];

        return Create(issuer ?? string.Empty, audience ?? string.Empty, signingKey ?? string.Empty);
    }

    public static JwtSettings Create(string issuer, string audience, string signingKey)
    {
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
        }

        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException(
                "Configure Jwt:SigningKey with at least 32 bytes using User Secrets or an environment variable.");
        }

        return new JwtSettings(issuer, audience, signingKey);
    }
}
