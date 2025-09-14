using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Database.Models.Mappers.BeatLeader;

public class PlatformMappers
{
    public static PlayerHardwareInfo.EPlatform Map(string platform)
        => platform switch
        {
            "steam" => PlayerHardwareInfo.EPlatform.Steam,
            "oculuspc" => PlayerHardwareInfo.EPlatform.MetaPC,
            "oculus" => PlayerHardwareInfo.EPlatform.MetaNative,
            _ => PlayerHardwareInfo.EPlatform.Unknown
        };
}