namespace GuildSaber.Common.Services.ScoreSaber.Models;

/// <remarks>
/// There isn't more Hmds than the ones listed here, the Quest2 and onward was added when they moved to the Device system.
/// </remarks>
public enum LegacyHMD
{
    Unknown = 0,
    Rift = 1,
    Vive = 2,
    VivePro = 4,
    WMR = 8,
    RiftS = 16,
    Quest = 32,
    Index = 64,
    ViveCosmos = 128
}