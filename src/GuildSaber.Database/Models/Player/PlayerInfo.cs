namespace GuildSaber.Database.Models.Player;

public readonly record struct PlayerInfo(
    string Username,
    string AvatarUrl,
    string Country
);