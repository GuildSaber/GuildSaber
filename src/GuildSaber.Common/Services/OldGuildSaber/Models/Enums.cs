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

[Flags]
public enum EState
{
    UnVerified = 0,
    Allowed = 1 << 0,
    Denied = 1 << 1,
    NeedConfirmation = 1 << 2,
    MinScoreRequirement = 1 << 3,
    ProhibitedModifiers = 1 << 4,
    MissingModifiers = 1 << 5,
    NewScore = 1 << 6,
    UpdatedScore = 1 << 7,
    TooManyPauses = 1 << 8,
    MissingScoreStatistics = 1 << 9,
    NoFullCombo = 1 << 10,
    ScoringTeamConfirmed = 1 << 11,
    ScoringTeamDenied = 1 << 12,
    MissingRequirements = TooManyPauses | MissingScoreStatistics | NoFullCombo
}