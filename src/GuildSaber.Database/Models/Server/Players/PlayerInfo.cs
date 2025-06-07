namespace GuildSaber.Database.Models.Server.Players;

public readonly record struct PlayerInfo(
    string Username,
    string AvatarUrl,
    string Country,
    DateTimeOffset CreatedAt
);