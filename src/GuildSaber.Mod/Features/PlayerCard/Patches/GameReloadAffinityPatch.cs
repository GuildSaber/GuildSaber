using GuildSaber.Mod.Features.PlayerCard.UI;
using SiraUtil.Affinity;

namespace GuildSaber.Mod.Features.PlayerCard.Patches;

/// <summary>
/// Moves the persistent player card into the active scene before Zenject disposes it during a game reload.
/// </summary>
public sealed class GameReloadAffinityPatch(PlayerCardView view) : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.RestartGame))]
    private void RestartGamePrefix() => view.MoveToActiveScene();
}