namespace GuildSaber.Mod.Features.GuildSaber.Runtime;

public abstract record GuildSaberRuntimeState
{
    public sealed record Loading : GuildSaberRuntimeState;
    public sealed record AccountRequired(BeatLeaderId BeatLeaderId) : GuildSaberRuntimeState;
    public sealed record NoGuilds : GuildSaberRuntimeState;
    public sealed record Ready(GuildSaberSnapshot Snapshot) : GuildSaberRuntimeState;

    public sealed record Failed(
        string Message,
        bool CanRetry = true
    ) : GuildSaberRuntimeState;
}