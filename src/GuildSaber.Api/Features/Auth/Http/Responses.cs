namespace GuildSaber.Api.Features.Auth.Http;

public static class AuthResponse
{
    public readonly record struct TokenResponse(string Token);
}