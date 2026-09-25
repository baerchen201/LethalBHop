using GameNetcodeStuff;
using HarmonyLib;

namespace LethalBHop.Patches;

[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ScrollMouse_performed))]
internal static class PlayerControllerB_ScrollMouse_performed
{
    private static bool Prefix()
    {
        return LethalBHop.Instance.AutoBhop;
    }
}
