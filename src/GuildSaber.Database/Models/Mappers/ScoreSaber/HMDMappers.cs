using GuildSaber.Common.Services.ScoreSaber.Models;
using GuildSaber.Database.Models.Server.Players;

namespace GuildSaber.Database.Models.Mappers.ScoreSaber;

public static class HMDMappers
{
    public static PlayerHardwareInfo.EHMD Map(this HMD self, string? deviceHmd)
        => (self, deviceHmd) switch
        {
            (HMD.Rift, _) _ => PlayerHardwareInfo.EHMD.Rift,
            (HMD.Vive, _) => PlayerHardwareInfo.EHMD.Vive,
            (HMD.VivePro, _) => PlayerHardwareInfo.EHMD.VivePro,
            (HMD.WMR, _) => PlayerHardwareInfo.EHMD.WMR,
            (HMD.RiftS, _) => PlayerHardwareInfo.EHMD.RiftS,
            (HMD.Quest, _) => PlayerHardwareInfo.EHMD.Quest,
            (HMD.Index, _) => PlayerHardwareInfo.EHMD.Index,
            (HMD.ViveCosmos, _) => PlayerHardwareInfo.EHMD.ViveCosmos,
            (HMD.Unknown, "Quest 2") => PlayerHardwareInfo.EHMD.Quest2,
            (HMD.Unknown, "Quest 3") => PlayerHardwareInfo.EHMD.Quest3,
            (HMD.Unknown, "Quest 3S") => PlayerHardwareInfo.EHMD.Quest3S,
            _ => PlayerHardwareInfo.EHMD.Unknown
        };
}