// https://github.com/aIIison/lcbhop/blob/8d4d12a7684c8920b43465e3aabd6dda22cc6865/src/CPMPlayer.cs

/*
 * Based on https://github.com/WiggleWizard/quake3-movement-unity3d/blob/master/CPMPlayer.cs
 * Modified to match https://github.com/ValveSoftware/halflife/blob/master/pm_shared/pm_shared.c
 */

using GameNetcodeStuff;
using LethalBHop.Patches;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalBHop;

public class CPMPlayer : MonoBehaviour
{
    // Cache all config values here as fields to maximize performance
    internal static bool autobhop;
    internal static bool speedometer;
    internal static bool enablebunnyhopping;

    /* Frame occuring factors */
    internal static float gravity; // Gravity

    internal static float friction; // Ground friction

    /* Movement stuff */
    internal static float maxspeed; // Max speed
    internal static float movespeed; // Ground speed (like cl_forwardspeed etc.)
    internal static float accelerate; // Ground acceleration
    internal static float airaccelerate; // Air acceleration
    internal static float stopspeed; // Ground deceleration

    // Contains the command the user wishes upon the character
    struct Cmd
    {
        public float forwardMove;
        public float rightMove;

        // we can remove upMove since it's never used
        // public float upMove;
    }

    public PlayerControllerB player = null!;
    private CharacterController _controller = null!;

    private Vector3 velocity = Vector3.zero;

    internal bool wishJump = false;

    // Player commands, stores wish commands that the player asks for (Forward, back, jump, etc)
    private Cmd _cmd;

    private GameObject compass = null!;
    private TextMeshProUGUI speedo = null!;

    private InputAction jumpAction = null!,
        switchItemAction = null!;

    private void Start()
    {
        _controller = player.thisController;
        var actions = InputSystem.actions;
        jumpAction = actions.FindAction("Jump");
        switchItemAction = actions.FindAction("SwitchItem");
    }

    private void Update()
    {
#if DEBUG
        LethalBHop.Logger.LogDebug($">> {nameof(Update)}");
#endif
        // Disables fall damage
        player.fallValue = player.fallValueUncapped = 0.0f;
        // Disable stamina
        player.sprintMeter = 1.0f;

        if (
            !player.IsOwner
            || !player.isPlayerControlled
            || (player.IsServer && !player.isHostPlayerObject)
        )
        {
#if DEBUG
            LethalBHop.Logger.LogDebug($"<< {nameof(Update)} not a controllable player");
#endif
            return;
        }
        if (
            player.quickMenuManager.isMenuOpen // ik this causes problems but lets keep it for the funnies :)
            || player.inSpecialInteractAnimation
            || player.isTypingChat
        )
        {
#if DEBUG
            LethalBHop.Logger.LogDebug($"<< {nameof(Update)} controls disabled");
#endif
            return;
        }

        // Don't patch movement on ladders
        if (player.isClimbingLadder)
        {
#if DEBUG
            LethalBHop.Logger.LogDebug($"<< {nameof(Update)} on ladder");
#endif
            CharacterController_Move.allowMove = true;
            return;
        }

        /* Movement, here's the important part */
        Jump();

        if (!wishJump && _controller.isGrounded)
            Friction();

        if (_controller.isGrounded)
            WalkMove();
        else if (!_controller.isGrounded)
            AirMove();

        // Move the controller
#if DEBUG
        LethalBHop.Logger.LogDebug(" - Applying movement");
#endif
        CharacterController_Move.allowMove = true; // Disable the Move Patch
        _controller.Move(velocity / 32.0f * Time.deltaTime);
        CharacterController_Move.allowMove = false; // Reenable the Move Patch

        wishJump = false;

        /* Speedometer */
        if (speedometer)
        {
#if DEBUG
            LethalBHop.Logger.LogDebug(" - Updating speedometer");
#endif
            if (!compass)
            {
#if DEBUG
                LethalBHop.Logger.LogDebug(" - Obtaining speedometer object");
#endif
                compass = GameObject.Find(
                    "/Systems/UI/Canvas/IngamePlayerHUD/TopLeftCorner/Compass"
                );
                speedo = compass.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (!compass)
            {
                LethalBHop.Logger.LogWarning(
                    "Speedometer object is null, temporarily disabling speedometer"
                );
                speedometer = false;
                return;
            }

            compass.SetActive(true);

            // Only X, Y speed
            Vector3 vel = velocity;
            vel.y = 0.0f;

            speedo.text = $"{(int)vel.magnitude} u";
            speedo.rectTransform.sizeDelta = speedo.GetPreferredValues();
        }
        else
        {
            if (compass)
                compass.SetActive(false);
        }
    }

    /*******************************************************************************************************\
    |* MOVEMENT
    \*******************************************************************************************************/

    /*
     * Sets the movement direction based on player input
     */
    private void SetMovementDir()
    {
        _cmd.forwardMove = player.playerActions.Movement.Move.ReadValue<Vector2>().y * movespeed;
        _cmd.rightMove = player.playerActions.Movement.Move.ReadValue<Vector2>().x * movespeed;
    }

    /*
     * Checks for jump input
     */
    private void Jump()
    {
#if DEBUG
        LethalBHop.Logger.LogDebug($">> {nameof(Jump)} {nameof(wishJump)}:{wishJump}");
#endif
        if (autobhop)
        {
            wishJump = jumpAction.ReadValue<float>() > 0.0f;
#if DEBUG
            LethalBHop.Logger.LogDebug(
                $"<< {nameof(Jump)} {nameof(autobhop)} {nameof(wishJump)}:{wishJump}"
            );
#endif
        }
        else
        {
            if (!wishJump)
                wishJump = switchItemAction.ReadValue<float>() != 0.0f;
#if DEBUG
            LethalBHop.Logger.LogDebug(
                $"<< {nameof(Jump)} !{nameof(autobhop)} {nameof(wishJump)}:{wishJump}"
            );
#endif
        }

        if (!enablebunnyhopping)
            PreventMegaBunnyJumping();
    }

    /*
     * Execs when the player is in the air
     */
    private void AirMove()
    {
#if DEBUG
        LethalBHop.Logger.LogDebug(
            $">> {nameof(AirMove)} {nameof(gravity)}:{gravity} {nameof(airaccelerate)}:{airaccelerate}"
        );
#endif
        Vector3 wishvel;
        Vector3 wishdir;
        float wishspeed;

        SetMovementDir();

        wishvel = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishvel = transform.TransformDirection(wishvel);

        wishdir = wishvel;

        wishspeed = wishdir.magnitude;
        wishdir.Normalize();

        if (wishspeed > maxspeed)
        {
            wishvel *= maxspeed / wishspeed;
            wishspeed = maxspeed;
        }

        AirAccelerate(wishdir, wishspeed, airaccelerate);

        // Apply gravity
        velocity.y -= gravity * Time.deltaTime;
    }

    /*
     * Called every frame when the engine detects that the player is on the ground
     */
    private void WalkMove()
    {
#if DEBUG
        LethalBHop.Logger.LogDebug(
            $">> {nameof(WalkMove)} {nameof(movespeed)}:{movespeed} {nameof(accelerate)}:{accelerate}"
        );
#endif
        Vector3 wishvel;
        Vector3 wishdir;
        float wishspeed;

        SetMovementDir();

        wishvel = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishvel = transform.TransformDirection(wishvel);

        wishdir = wishvel;

        wishspeed = wishdir.magnitude;
        wishdir.Normalize();

        if (wishspeed > maxspeed)
        {
            wishvel *= maxspeed / wishspeed;
            wishspeed = maxspeed;
        }

        Accelerate(wishdir, wishspeed, accelerate);

        // Reset the gravity velocity
        velocity.y = -gravity * Time.deltaTime;

        if (wishJump)
        {
            velocity.y = 295;

            player.PlayJumpAudio();
            player.PlayerJumpedServerRpc();
            // Animate player jumping, this is a bit tricky since its a private method (there's probably a better way to do this)
            //   ^ We use a publicized assembly, we can just call it directly.
#if false
                // XXX: This messes with the animator and makes you not be able to crouch, couldnt figure it out yet!
                 PlayerControllerB_Jump_performed.allowJump = true; // Disable jump patch
                 player.Jump_performed(new InputAction.CallbackContext());
                 PlayerControllerB_Jump_performed.allowJump = false; // Reenable jump patch
#endif
        }
    }

    /*
     * Applies friction to the player, called in both the air and on the ground
     */
    private void Friction()
    {
#if DEBUG
        LethalBHop.Logger.LogDebug($">> {nameof(Friction)} {nameof(friction)}:{friction}");
#endif
        Vector3 vec = velocity;
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.magnitude;

        if (speed < 0.1f)
            return;

        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (_controller.isGrounded)
        {
            control = (speed < stopspeed) ? stopspeed : speed;
            drop += control * friction * Time.deltaTime;
        }

        newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;

        newspeed /= speed;

        velocity.x *= newspeed;
        velocity.z *= newspeed;
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(velocity, wishdir);

        addspeed = wishspeed - currentspeed;

        if (addspeed <= 0)
            return;

        accelspeed = accel * Time.deltaTime * wishspeed;

        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }

    private void AirAccelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;
        float wishspd = wishspeed;

        if (wishspd > 30)
            wishspd = 30;

        currentspeed = Vector3.Dot(velocity, wishdir);

        addspeed = wishspd - currentspeed;

        if (addspeed <= 0)
            return;

        accelspeed = accel * wishspeed * Time.deltaTime;

        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }

    private void PreventMegaBunnyJumping()
    {
#if DEBUG
        LethalBHop.Logger.LogDebug($">> {nameof(PreventMegaBunnyJumping)}");
#endif
        Vector3 vec = velocity;
        float spd;
        float fraction;
        float maxscaledspeed;

        maxscaledspeed =
            1.7f /* BUNNYJUMP_MAX_SPEED_FACTOR */
            * maxspeed;

        if (maxscaledspeed <= 0.0f)
            return;

        vec.y = 0.0f;
        spd = vec.magnitude;

        if (spd <= maxscaledspeed)
            return;

        fraction = (maxscaledspeed / spd) * 0.65f;

        velocity.x *= fraction;
        velocity.z *= fraction;
    }
}
