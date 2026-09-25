using HarmonyLib;
using UnityEngine;

namespace LethalBHop.Patches;

[HarmonyPatch(typeof(CharacterController), nameof(CharacterController.Move))]
internal static class CharacterController_Move
{
    internal static bool allowMove;

    private static bool Prefix()
    {
        return allowMove;
    }
}
