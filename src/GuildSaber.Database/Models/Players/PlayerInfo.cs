namespace GuildSaber.Database.Models.Players;

public readonly record struct PlayerInfo(
    string Username,
    string AvatarUrl,
    string Country
);