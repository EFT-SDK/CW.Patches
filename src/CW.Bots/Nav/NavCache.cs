using System;
using System.IO;
using UnityEngine;

namespace CW.Bots.Nav
{
    internal static class NavCache
    {
        private const string Magic = "CWNAV";

        internal static string Dir
        {
            get { return Path.Combine(BepInEx.Paths.ConfigPath, Path.Combine("cw_bots", "nav")); }
        }

        internal static string PathFor(string map)
        {
            string safe = map;
            char[] bad = Path.GetInvalidFileNameChars();
            for (int i = 0; i < bad.Length; i++) safe = safe.Replace(bad[i], '_');
            return Path.Combine(Dir, safe + ".nav");
        }

        internal static bool Exists(string map)
        {
            return !string.IsNullOrEmpty(map) && File.Exists(PathFor(map));
        }

        internal static void Save(NavGrid g)
        {
            try
            {
                if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
                using (var fs = new FileStream(PathFor(g.Map), FileMode.Create, FileAccess.Write))
                using (var w = new BinaryWriter(fs))
                {
                    w.Write(Magic.ToCharArray());
                    w.Write(NavBaker.Version);
                    w.Write(g.Map);
                    w.Write(g.Cell);
                    WriteV3(w, g.Min);
                    WriteV3(w, g.Max);

                    w.Write(g.Count);
                    for (int i = 0; i < g.Count; i++) { WriteV3(w, g.Pos[i]); w.Write(g.CoverLow[i]); w.Write(g.CoverHigh[i]); }

                    w.Write(g.LinkCount);
                    for (int i = 0; i <= g.Count; i++) w.Write(g.LinkStart[i]);
                    for (int i = 0; i < g.LinkCount; i++) { w.Write(g.LinkTo[i]); w.Write(g.LinkFlags[i]); }
                }
                Plugin.Log.LogInfo("nav saved: " + PathFor(g.Map) + " (" + g.Count + " nodes)");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("nav save failed: " + e.Message);
            }
        }

        internal static NavGrid Load(string map)
        {
            string path = PathFor(map);
            if (!File.Exists(path)) return null;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var r = new BinaryReader(fs))
                {
                    string magic = new string(r.ReadChars(Magic.Length));
                    if (magic != Magic) { Plugin.Log.LogWarning("nav cache bad magic: " + path); return null; }
                    int ver = r.ReadInt32();
                    if (ver != NavBaker.Version) { Plugin.Log.LogWarning("nav cache version " + ver + " != " + NavBaker.Version + ", rebaking"); return null; }

                    var g = new NavGrid();
                    g.Map = r.ReadString();
                    g.Cell = r.ReadSingle();
                    g.Min = ReadV3(r);
                    g.Max = ReadV3(r);

                    int n = r.ReadInt32();
                    g.Pos = new Vector3[n];
                    g.CoverLow = new byte[n];
                    g.CoverHigh = new byte[n];
                    for (int i = 0; i < n; i++) { g.Pos[i] = ReadV3(r); g.CoverLow[i] = r.ReadByte(); g.CoverHigh[i] = r.ReadByte(); }

                    int links = r.ReadInt32();
                    g.LinkStart = new int[n + 1];
                    for (int i = 0; i <= n; i++) g.LinkStart[i] = r.ReadInt32();
                    g.LinkTo = new int[links];
                    g.LinkFlags = new byte[links];
                    for (int i = 0; i < links; i++) { g.LinkTo[i] = r.ReadInt32(); g.LinkFlags[i] = r.ReadByte(); }

                    g.BuildIndex();
                    return g;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("nav load failed: " + e.Message);
                return null;
            }
        }

        internal static bool Delete(string map)
        {
            try
            {
                string p = PathFor(map);
                if (!File.Exists(p)) return false;
                File.Delete(p);
                return true;
            }
            catch { return false; }
        }

        private static void WriteV3(BinaryWriter w, Vector3 v) { w.Write(v.x); w.Write(v.y); w.Write(v.z); }
        private static Vector3 ReadV3(BinaryReader r) { return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()); }
    }
}
