using System.Reflection;
using HarmonyLib;

namespace CW.ClientEmu.Patches
{
    [HarmonyPatch]
    internal static class Patch_ExitButtonLatch
    {
        private static FieldInfo _instance, _alreadyQuit;

        private static MethodBase TargetMethod()
        {
            var gui = AccessTools.TypeByName("MainGUI");
            _instance = AccessTools.Field(gui, "Instance");
            _alreadyQuit = AccessTools.Field(gui, "_alreadyQuit");
            return AccessTools.Method(AccessTools.TypeByName("Peer"), "OnDisconnect");
        }

        private static void Postfix()
        {
            if (_instance == null || _alreadyQuit == null) return;
            try
            {
                var gui = _instance.GetValue(null);
                if (gui == null) return;
                if (!(bool)_alreadyQuit.GetValue(gui)) return;
                _alreadyQuit.SetValue(gui, false);
                Plugin.Log.LogInfo("exit-server button latch reset");
            }
            catch { }
        }
    }
}
