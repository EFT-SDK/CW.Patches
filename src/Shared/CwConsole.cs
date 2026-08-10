using System.Reflection;
using HarmonyLib;

namespace CW.Shared
{
    internal static class CwConsole
    {
        private static MethodInfo _print;
        private static bool _resolved;

        internal static void Print(string message)
        {
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    var t = AccessTools.TypeByName("Console");
                    if (t != null)
                    {
                        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                        {
                            if (m.Name != "print") continue;
                            var ps = m.GetParameters();
                            if (ps.Length == 1 && ps[0].ParameterType == typeof(string)) { _print = m; break; }
                        }
                    }
                }
                if (_print != null) _print.Invoke(null, new object[] { message });
            }
            catch { }
        }
    }
}
