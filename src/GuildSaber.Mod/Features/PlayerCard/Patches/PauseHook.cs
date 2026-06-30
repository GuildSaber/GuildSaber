using System;
using HarmonyLib;

namespace GuildSaber.Mod.Features.PlayerCard.Patches;

public class PauseHook
{
    public static Action EventGamePaused = () => {};
    public static Action EventGameResumed = () => { };
}

[HarmonyPatch(typeof(GamePause), nameof(GamePause.Pause))]
public class GamePausePatch
{
    public static void Prefix()
    {
        PauseHook.EventGamePaused.Invoke();
    }
}

[HarmonyPatch(typeof(GamePause), nameof(GamePause.Resume))]
public class GameResumePatch
{
    public static void Prefix()
    {
        PauseHook.EventGameResumed.Invoke();
    }
}