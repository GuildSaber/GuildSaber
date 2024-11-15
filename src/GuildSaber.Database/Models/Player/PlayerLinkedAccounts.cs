using GuildSaber.Database.Models.StrongTypes.Implements;
using GuildSaber.Database.Models.StrongTypes.Others;

namespace GuildSaber.Database.Models.Player;

public readonly record struct PlayerLinkedAccounts(
    BeatLeaderId BeatLeaderId,
    ScoreSaberId? ScoreSaberId,
    DiscordId? DiscordId
);