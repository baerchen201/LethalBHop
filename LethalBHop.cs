using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace LethalBHop;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class LethalBHop : BaseUnityPlugin
{
    public static LethalBHop Instance { get; private set; } = null!;
    internal static new ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony Harmony = null!;

    #region Configs


    public ConfigEntry<bool> autobhop = null!;
    public bool AutoBhop => autobhop.Value;
    public ConfigEntry<bool> speedometer = null!;
    public bool Speedometer => speedometer.Value;
    public ConfigEntry<bool> enablebunnyhopping = null!;
    public bool EnableBunnyHopping => enablebunnyhopping.Value;

    public ConfigEntry<float> gravity = null!;
    public float Gravity => gravity.Value;
    public ConfigEntry<float> friction = null!;
    public float Friction => friction.Value;
    public ConfigEntry<float> maxspeed = null!;
    public float MaxSpeed => maxspeed.Value;
    public ConfigEntry<float> movespeed = null!;
    public float MoveSpeed => movespeed.Value;
    public ConfigEntry<float> accelerate = null!;
    public float Accelerate => accelerate.Value;
    public ConfigEntry<float> airaccelerate = null!;
    public float AirAccelerate => airaccelerate.Value;
    public ConfigEntry<float> stopspeed = null!;
    public float StopSpeed => stopspeed.Value;
    public ConfigEntry<float> jumpvelocity = null!;
    public float JumpVelocity => jumpvelocity.Value;

    #endregion

    internal static CPMPlayer player = null!;

    private void Awake()
    {
        const string SECTION_GENERAL = "General";
        const string SECTION_MOVEVARS = "MoveVars";

        Logger = base.Logger;
        Instance = this;

        autobhop = Config.Bind(
            SECTION_GENERAL,
            nameof(AutoBhop),
            false,
            "Disabling rebinds jump to scroll, needs ItemQuickSwitch mod!"
        );
        speedometer = Config.Bind(
            SECTION_GENERAL,
            nameof(Speedometer),
            true,
            "Enables speedometer HUD."
        );
        enablebunnyhopping = Config.Bind(
            SECTION_GENERAL,
            nameof(EnableBunnyHopping),
            true,
            "Disables the speed cap."
        );

        gravity = Config.Bind(SECTION_MOVEVARS, nameof(Gravity), 800.0f, "Gravity.");
        friction = Config.Bind(SECTION_MOVEVARS, nameof(Friction), 4.0f, "Ground friction.");
        maxspeed = Config.Bind(SECTION_MOVEVARS, nameof(MaxSpeed), 320.0f, "Max speed per tick.");
        movespeed = Config.Bind(
            SECTION_MOVEVARS,
            nameof(MoveSpeed),
            250.0f,
            "Ground speed (like cl_forwardspeed etc.)."
        );
        accelerate = Config.Bind(
            SECTION_MOVEVARS,
            nameof(Accelerate),
            10.0f,
            "Ground acceleration."
        );
        airaccelerate = Config.Bind(
            SECTION_MOVEVARS,
            nameof(AirAccelerate),
            20.0f,
            "Air acceleration."
        );
        stopspeed = Config.Bind(SECTION_MOVEVARS, nameof(StopSpeed), 75.0f, "Ground deceleration.");
        jumpvelocity = Config.Bind(SECTION_MOVEVARS, nameof(JumpVelocity), 295.0f, "Jump height.");

        Config.SettingChanged += UpdateConfigs;
        UpdateConfigs(null!, null!);

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Logger.LogDebug("Patching...");
        Harmony.PatchAll();
        Logger.LogDebug("Finished patching!");

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    private void UpdateConfigs(object sender, SettingChangedEventArgs e)
    {
        CPMPlayer.autobhop = AutoBhop;
        CPMPlayer.speedometer = Speedometer;
        CPMPlayer.enablebunnyhopping = EnableBunnyHopping;

        CPMPlayer.gravity = Gravity;
        CPMPlayer.friction = Friction;
        CPMPlayer.maxspeed = MaxSpeed;
        CPMPlayer.movespeed = MoveSpeed;
        CPMPlayer.accelerate = Accelerate;
        CPMPlayer.airaccelerate = AirAccelerate;
        CPMPlayer.stopspeed = StopSpeed;
        CPMPlayer.jumpvelocity = JumpVelocity;
    }
}
