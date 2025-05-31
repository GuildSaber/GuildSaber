using System.Diagnostics.CodeAnalysis;

namespace GuildSaber.Api.Features.Auth;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class AuthConstants
{
    public const string PlayerIdClaimType = "PlayerId";
    public const string ManagerClaimType = "Manager";
    public const string GuildPermissionClaimPrefix = "Guild_";
    public static string GuildPermissionClaimType(string guildId) => $"{GuildPermissionClaimPrefix}{guildId}";
}