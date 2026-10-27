using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Database.Models.Mappers.BeatLeader;

public class PlatformMappers
{
    public static Player.EPlatform Map(string platform) => platform switch
    {
        "steam" => Player.EPlatform.Steam,
        "oculuspc" => Player.EPlatform.MetaPC,
        "oculus" => Player.EPlatform.MetaNative,
        _ => Player.EPlatform.Unknown
    };
}