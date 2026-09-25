using GameNetcodeStuff;
using HarmonyLib;

namespace LethalBHop.Patches;

[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Jump_performed))]
internal static class PlayerControllerB_Jump_performed
{
#if false
    internal static bool allowJump;
#endif

    private static bool Prefix()
    {
        LethalBHop.player?.wishJump = true;
#if false
        return allowJump;
#else
        return false;
#endif
    }
}
