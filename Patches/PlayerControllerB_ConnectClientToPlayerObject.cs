using GameNetcodeStuff;
using HarmonyLib;

namespace LethalBHop.Patches;

[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
internal static class PlayerControllerB_ConnectClientToPlayerObject
{
    private static void Postfix(ref PlayerControllerB __instance)
    {
        (LethalBHop.player = __instance.gameObject.AddComponent<CPMPlayer>()).player = __instance;
    }
}
