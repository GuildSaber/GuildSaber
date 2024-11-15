namespace GuildSaber.Database.Models.Guild;

public readonly record struct GuildJoinRequirements(
    GuildJoinRequirements.ERequirements Requirements,
    uint MinRank,
    uint MaxRank,
    uint MinPP,
    uint MaxPP,
    uint AccountAgeUnix)
{
    [Flags]
    public enum ERequirements
    {
        None = 0,
        Submission = 1 << 0,
        MinRank = 1 << 1,
        MaxRank = 1 << 2,
        MinPP = 1 << 3,
        MaxPP = 1 << 4,
        AccountAgeUnix = 1 << 5
    }
}