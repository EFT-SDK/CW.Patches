using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CW.Snow
{
    [BepInPlugin(Guid, "CW Snow", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.snow";
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Report snow weather on every server you host.");
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
