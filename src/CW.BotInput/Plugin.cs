using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CW.BotInput
{
    [BepInPlugin(Guid, "CW Bot Input", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.botinput";
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
