namespace GuildSaber.Common.Services.OldGuildSaber.Models;

[Flags]
public enum ERequirements
{
    None = 0,
    NeedAdminConfirmation = 1 << 0,
    FullCombo = 1 << 1,
    MaxPauses = 1 << 2,
    NeedScoreStatistics = 1 << 3
}