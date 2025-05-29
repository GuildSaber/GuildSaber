using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GuildSaber.Api.Features.Auth.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GuildSaber.Api.Features.Auth;

public class JwtService(IOptions<JwtAuthSettings> authSettings)
{
    private readonly JwtAuthSettings _autSettings = authSettings.Value;
    public readonly record struct JwtTokenInfo(string Token, Guid Identifier, DateTime IssuedAt, DateTime ExpireAt);

    public JwtTokenInfo CreateToken(TimeSpan expiration)
    {
        var identifier = Guid.NewGuid();
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _autSettings.Issuer,
            Audience = _autSettings.Audience,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Jti, identifier.ToString())
            ]),
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.Add(expiration),
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