using SiraUtil.Affinity;

namespace GuildSaber.Mod.Features.PlayerCard.Patches;

/// <summary>
/// Patches that causes the player card to only be visible when the game is paused.
/// </summary>
public class PauseHookAffinityPatch(PlayerCardManager manager) : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(GamePause), nameof(GamePause.Pause))]
    private void PausePrefix() => manager.SetFloatingScreenActive(true);


    [AffinityPrefix]
    [AffinityPatch(typeof(GamePause), nameof(GamePause.WillResume))]
    private void WillResumePrefix() => manager.SetFloatingScreenActive(false);
}