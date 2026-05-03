using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GuildSaber.Mod.Core.PlayerCard.UI;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace GuildSaber.Mod.Harmony;

/// <summary>
/// This card is needed because the card uses DontDestroyOnLoad (to be kept as a singleton across scenes).
/// When the game reloads, the card need to be moved back to the active scene so zenject can dispose it correctly
/// before it's complete reloading, which recreates all the new instances.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[HarmonyPatch(typeof(MenuTransitionsHelper), nameof(MenuTransitionsHelper.RestartGame))]
public class GameReloadPatch
{
    public static void Prefix()
    {
        var cardArray = UnityEngine.Resources.FindObjectsOfTypeAll<PlayerCardView>();

        // Ensure only one card exists.
        var card = cardArray.SingleOrDefault();
        if (card == null)
            return;

        SceneManager.MoveGameObjectToScene(card.transform.parent.gameObject, SceneManager.GetActiveScene());
    }
}