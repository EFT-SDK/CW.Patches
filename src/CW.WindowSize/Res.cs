using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CW.WindowSize
{
    internal static class Res
    {
        private static MethodInfo _getUserInfo;
        private static FieldInfo _settingsField;
        private static FieldInfo _resolutionField;

        internal static void Init()
        {
            _getUserInfo = AccessTools.Method(AccessTools.TypeByName("Main"), "get_UserInfo");
            _settingsField = AccessTools.Field(AccessTools.TypeByName("UserInfo"), "settings");
            _resolutionField = AccessTools.Field(AccessTools.TypeByName("UserSettings"), "resolution");
        }

        internal static bool TrySettingsResolution(out int w, out int h)
        {
            w = 0; h = 0;
            try
            {
                var user = _getUserInfo.Invoke(null, null);
                if (user == null) return false;
                var settings = _settingsField.GetValue(user);
                if (settings == null) return false;
                var res = (Resolution)_resolutionField.GetValue(settings);
                w = res.width; h = res.height;
                return w > 0 && h > 0;
            }
            catch { return false; }
        }
    }
}
