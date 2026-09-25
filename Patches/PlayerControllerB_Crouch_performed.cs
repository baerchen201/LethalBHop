using System.Collections.Generic;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LethalModUtils;
using UnityEngine;

namespace LethalBHop.Patches;

[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Crouch_performed))]
internal static class PlayerControllerB_Crouch_performed
{
    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions
    )
    {
        return new CodeMatcher(instructions)
            .MatchForward(
                new CodeMatch(
                    OpCodes.Callvirt,
                    AccessTools.PropertyGetter(
                        typeof(CharacterController),
                        nameof(CharacterController.isGrounded)
                    )
                )
            )
            .SetInstruction(
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(PlayerControllerB_Crouch_performed),
                        nameof(IsGroundedReplacement)
                    )
                )
            )
            .MatchForward(
                new CodeMatch(
                    OpCodes.Ldfld,
                    AccessTools.Field(
                        typeof(PlayerControllerB),
                        nameof(PlayerControllerB.isJumping)
                    )
                )
            )
            .SetInstruction(
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(PlayerControllerB_Crouch_performed),
                        nameof(IsJumpingReplacement)
                    )
                )
            )
            .InstructionEnumeration();
    }

    private static bool IsGroundedReplacement(CharacterController controller)
    {
        return true;
    }

    private static bool IsJumpingReplacement(PlayerControllerB player)
    {
        return false;
    }
}
