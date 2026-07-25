using SiraUtil.Affinity;

namespace GuildSaber.Mod.Features.PlayerCard.Patches;

/// <summary>
/// Shows the in-song player card only while the game is paused.
/// </summary>
public sealed class PauseAffinityPatch(PlayerCardManager manager) : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(GamePause), nameof(GamePause.Pause))]
    private void PausePrefix() => manager.SetPaused(true);

    [AffinityPrefix]
    [AffinityPatch(typeof(GamePause), nameof(GamePause.WillResume))]
    private void WillResumePrefix() => manager.SetPaused(false);
}