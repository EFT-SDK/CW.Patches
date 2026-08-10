using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace CW.ClientEmu
{
    [BepInPlugin(Guid, "CW Client Emu", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.clientemu";
        internal static ManualLogSource Log;
        internal static ConfigEntry<string> Host;

        private void Awake()
        {
            Log = Logger;
            Host = Config.Bind("General", "Host", "cw.servphcorpp.com:8099",
                "Backend host[:port] every database request is redirected to (replaces the baked-in databaseIP).");
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
