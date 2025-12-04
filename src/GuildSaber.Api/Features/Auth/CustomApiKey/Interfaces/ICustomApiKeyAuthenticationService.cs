using System.Net;
using GuildSaber.Api.Features.Auth.CustomApiKey.ValidationTypes;
using Microsoft.AspNetCore.Authentication;

namespace GuildSaber.Api.Features.Auth.CustomApiKey.Interfaces;

public interface ICustomApiKeyAuthenticationService
{
    Task<AuthenticateResult> AuthenticateAsync(BasicCredential credential, IPAddress? clientIp);
}