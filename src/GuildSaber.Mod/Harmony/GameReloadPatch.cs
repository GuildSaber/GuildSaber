using System.Linq;
using GuildSaber.Mod.Core.PlayerCard.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuildSaber.Mod.Harmony;

[HarmonyPatch(typeof(SettingsNavigationController), "HandleFinishButton")]
public class GameReloadPatch
{

    public static void Prefix(SettingsNavigationController __instance, ref SettingsNavigationController.FinishAction finishAction)
    {
        var cardArray = UnityEngine.Resources.FindObjectsOfTypeAll<PlayerCardView>();
        if (cardArray.Any())
        {
            var card = cardArray.First();
            
            //SceneManager.MoveGameObjectToScene(card.transform.gameObject, SceneManager.GetActiveScene());
            SceneManager.MoveGameObjectToScene(card.transform.parent.gameObject, SceneManager.GetActiveScene());
        }
    } 
    
}