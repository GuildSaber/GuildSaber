using System.Linq;
using GuildSaber.Api.Features.Guilds.Members.LevelStats;
using GuildSaber.Api.Features.Players;
using UnityEngine;

namespace GuildSaber.Mod.Core.PlayerCard;

public static class PlayerCardLibrary
{
    public static bool CanPlayerUseCustomColors(LevelStatResponses.MemberLevelStat[] levels,
                                                PlayerResponses.Player player)
    {
        
        return levels.Where(x => x.Level.CategoryId == null && !x.IsLocked)
                .LastOrDefault(x => x.IsCompleted).Level
            .Order >= 30 || player.PlayerLinkedAccounts.BeatLeaderId == "76561198846350061" 
                         || player.PlayerLinkedAccounts.BeatLeaderId == "76561198126131670";
    }

    public static Color FromArgb(int argb)
    {
        var r = (byte)(argb >> 16 & 0xFF);
        var g = (byte)(argb >> 8 & 0xFF);
        var b = (byte)(argb & 0xFF);
        return new Color(r, g, b);
    }
    
}