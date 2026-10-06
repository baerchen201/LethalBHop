using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LethalBHop.Patches;

[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.TeleportPlayer))]
internal static class PlayerControllerB_TeleportPlayer
{
    private static void Postfix(ref PlayerControllerB __instance)
    {
        if (LethalBHop.Instance.StopOnTeleport && __instance.IsOwner)
            LethalBHop.player?.velocity = Vector3.zero;
    }
}
