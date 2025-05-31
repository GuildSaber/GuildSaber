using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GuildSaber.Api.Features.Auth.Settings;
using GuildSaber.Database.Models.StrongTypes;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GuildSaber.Api.Features.Auth.Sessions;

public class JwtService(IOptions<JwtAuthSettings> authSettings, TimeProvider timeProvider)
{
    private readonly JwtAuthSettings _autSettings = authSettings.Value;

    public readonly record struct JwtTokenInfo(
        string Token,
        UuidV7 Identifier,
        DateTimeOffset IssuedAt,
        DateTimeOffset ExpireAt);

    public JwtTokenInfo CreateToken(TimeSpan expiration)
    {
        var utcNow = timeProvider.GetUtcNow();
        var expireAt = utcNow.Add(expiration);
        var identifier = UuidV7.Create(utcNow);
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _autSettings.Issuer,
            Audience = _autSettings.Audience,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Jti, identifier.ToString())
            ]),
            IssuedAt = utcNow.DateTime,
            Expires = expireAt.DateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_autSettings.Secret)),
                SecurityAlgorithms.HmacSha256
            )
        };

        return new JwtTokenInfo(
            tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor)),
            identifier,
            tokenDescriptor.IssuedAt.Value,
            tokenDescriptor.Expires.Value
        );
    }
}