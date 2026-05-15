using System.Collections.Generic;
using System.Linq;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Players;
using UnityEngine;

namespace GuildSaber.Mod.Features.PlayerCard;

public static class PlayerCardLibrary
{
    public static bool CanPlayerUseCustomColors(
        IEnumerable<LevelStatResponses.MemberLevelStat> memberLevelStats, PlayerResponses.Player player)
    {
        const int requiredLevel = 30;
        return memberLevelStats.Where(x => x.Level.CategoryId is null && !x.IsLocked)
                .LastOrDefault(x => x.IsCompleted)?.Level.Order switch
            {
                _ when (ulong)player.PlayerLinkedAccounts.BeatLeaderId is 76561198846350061 or 76561198126131670
                    => true,
                >= requiredLevel => true,
                _ => false
            };
    }

    public static Color FromArgb(int argb)
    {
        var r = (byte)(argb >> 16 & 0xFF);
        var g = (byte)(argb >> 8 & 0xFF);
        var b = (byte)(argb & 0xFF);
        return new Color(r / 255.0f, g / 255.0f, b / 255.0f);
    }
}