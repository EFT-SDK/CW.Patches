using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CW.ServerCmds
{
    [BepInPlugin(Guid, "CW Server Commands", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.servercmds";
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            ServerCmds.Init();
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
