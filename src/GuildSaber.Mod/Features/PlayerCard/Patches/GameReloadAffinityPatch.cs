using GuildSaber.Mod.Features.PlayerCard.UI;
using SiraUtil.Affinity;
using UnityEngine.SceneManagement;

namespace GuildSaber.Mod.Features.PlayerCard.Patches;

/// <summary>
/// This part is needed because the card uses DontDestroyOnLoad (to be kept as a singleton across scenes).
/// When the game reloads, the card need to be moved back to the active scene so zenject can dispose it correctly
/// before it's complete reloading, which recreates all the new instances.
/// </summary>
public class GameReloadAffinityPatch(PlayerCardView playerCardView) : IAffinity
{
    [AffinityPrefix]
    [AffinityPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.RestartGame))]
    private void RestartGamePrefix()
    {
        if (playerCardView == null) return;
        SceneManager.MoveGameObjectToScene(playerCardView.transform.parent.gameObject, SceneManager.GetActiveScene());
    }
}