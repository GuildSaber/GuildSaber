using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GuildSaber.Api.Features.Auth.Sessions;

public sealed class SessionTokenService(
    IOptions<SessionCookieAuthSettings> settings,
    TimeProvider timeProvider)
{
    private readonly TokenValidationParameters _tokenValidationParameters = new()
    {
        ValidIssuer = settings.Value.Issuer,
        ValidAudience = settings.Value.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.SigningKey)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ClockSkew = TimeSpan.Zero,
        AuthenticationType = SessionCookieDefaults.AuthenticationScheme
    };

    public readonly record struct SessionTokenInfo(
        string Token,
        UuidV7 Identifier,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpireAt
    );

    public SessionTokenInfo CreateToken(TimeSpan expiration)
    {
        var utcNow = timeProvider.GetUtcNow();
        var expireAt = utcNow.Add(expiration);
        var identifier = UuidV7.Create(utcNow);
        var signingKey = _tokenValidationParameters.IssuerSigningKey;
        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var token = tokenHandler.CreateJwtSecurityToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Value.Issuer,
            Audience = settings.Value.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Jti, identifier.ToString())]),
            IssuedAt = utcNow.UtcDateTime,
            Expires = expireAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        });

        return new SessionTokenInfo(
            Token: tokenHandler.WriteToken(token),
            Identifier: identifier,
            IssuedAt: utcNow,
            ExpireAt: expireAt
        );
    }

    public ClaimsPrincipal ValidateToken(string token) => new JwtSecurityTokenHandler { MapInboundClaims = false }
        .ValidateToken(token, _tokenValidationParameters, out _);
}