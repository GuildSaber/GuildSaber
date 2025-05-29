namespace GuildSaber.Api.Features.Auth;

public static class AuthResponse
{
    public readonly record struct TokenResponse(string Token);
}