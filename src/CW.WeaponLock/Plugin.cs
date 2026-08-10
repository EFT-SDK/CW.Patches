using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace CW.WeaponLock
{
    [BepInPlugin(Guid, "CW Weapon Lock", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.weaponlock";
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }
    }
}
