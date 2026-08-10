using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CW.WindowSize
{
    [BepInPlugin(Guid, "CW Window Size", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.windowsize";
        internal static ManualLogSource Log;
        private bool _appliedOnStartup;

        private void Awake()
        {
            Log = Logger;
            Res.Init();
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
        }

        private void Update()
        {
            if (_appliedOnStartup) return;
            int w, h;
            if (!Res.TrySettingsResolution(out w, out h)) return;
            _appliedOnStartup = true;
            if (!Screen.fullScreen && (Screen.width != w || Screen.height != h))
            {
                Screen.SetResolution(w, h, false);
            }
        }
    }
}
