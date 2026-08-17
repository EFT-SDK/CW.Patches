using System.Collections.Generic;
using UnityEngine;

namespace CW.Bots.Nav
{
    internal static class LinkFlag
    {
        internal const byte Walk = 0;
        internal const byte Jump = 1;
        internal const byte Drop = 2;
    }

    internal static class CoverKind
    {
        internal const int None = 0;
        internal const int Crouch = 1;
        internal const int Full = 2;
    }

    internal sealed class NavGrid
    {
        internal string Map = string.Empty;
        internal float Cell = 0.75f;
        internal Vector3 Min, Max;

        internal Vector3[] Pos;
        internal byte[] CoverLow;
        internal byte[] CoverHigh;
        internal int[] LinkStart;
        internal int[] LinkTo;
        internal byte[] LinkFlags;

        private Dictionary<int, List<int>> _buckets;
        private const float BucketSize = 4f;

        internal int Count { get { return Pos == null ? 0 : Pos.Length; } }
        internal int LinkCount { get { return LinkTo == null ? 0 : LinkTo.Length; } }

        internal static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        internal static readonly int[] DZ = { 1, 1, 0, -1, -1, -1, 0, 1 };

        internal static int Octant(float x, float z)
        {
            float deg = Mathf.Atan2(x, z) * Mathf.Rad2Deg;
            return Mathf.RoundToInt(deg / 45f) & 7;
        }

        internal int CoverAgainst(int node, Vector3 threat)
        {
            if (node < 0 || node >= Count) return CoverKind.None;
            Vector3 d = threat - Pos[node];
            if (d.x * d.x + d.z * d.z < 0.04f) return CoverKind.None;

            int o = Octant(d.x, d.z);
            int bit = 1 << o;
            if ((CoverHigh[node] & bit) != 0) return CoverKind.Full;
            if ((CoverLow[node] & bit) != 0) return CoverKind.Crouch;
            return CoverKind.None;
        }

        internal int Enclosure(int node)
        {
            if (node < 0 || node >= Count) return 0;
            int m = CoverHigh[node];
            int n = 0;
            while (m != 0) { n += m & 1; m >>= 1; }
            return n;
        }

        internal void BuildIndex()
        {
            _buckets = new Dictionary<int, List<int>>(Count / 4 + 16);
            for (int i = 0; i < Count; i++)
            {
                int key = BucketKey(Pos[i]);
                List<int> list;
                if (!_buckets.TryGetValue(key, out list))
                {
                    list = new List<int>(8);
                    _buckets[key] = list;
                }
                list.Add(i);
            }
        }

        private static int Key(int bx, int bz) { return (bx * 73856093) ^ (bz * 19349663); }

        private static int BucketKey(Vector3 p)
        {
            return Key(Mathf.FloorToInt(p.x / BucketSize), Mathf.FloorToInt(p.z / BucketSize));
        }

        internal void Around(Vector3 p, float radius, List<int> outv)
        {
            outv.Clear();
            if (_buckets == null) return;

            int rings = Mathf.Max(1, Mathf.CeilToInt(radius / BucketSize));
            int cx = Mathf.FloorToInt(p.x / BucketSize);
            int cz = Mathf.FloorToInt(p.z / BucketSize);
            float sq = radius * radius;

            for (int dx = -rings; dx <= rings; dx++)
            {
                for (int dz = -rings; dz <= rings; dz++)
                {
                    List<int> list;
                    if (!_buckets.TryGetValue(Key(cx + dx, cz + dz), out list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        int n = list[i];
                        if ((Pos[n] - p).sqrMagnitude <= sq) outv.Add(n);
                    }
                }
            }
        }

        internal int Nearest(Vector3 p, float maxDist = 12f)
        {
            if (_buckets == null || Count == 0) return -1;
            int best = -1;
            float bestD = maxDist * maxDist;
            int rings = Mathf.Max(1, Mathf.CeilToInt(maxDist / BucketSize));
            int cx = Mathf.FloorToInt(p.x / BucketSize);
            int cz = Mathf.FloorToInt(p.z / BucketSize);

            for (int r = 0; r <= rings; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dz = -r; dz <= r; dz++)
                    {
                        if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                        List<int> list;
                        if (!_buckets.TryGetValue(Key(cx + dx, cz + dz), out list)) continue;
                        for (int i = 0; i < list.Count; i++)
                        {
                            int n = list[i];
                            Vector3 d = Pos[n] - p;
                            d.y *= 2f;
                            float sq = d.sqrMagnitude;
                            if (sq < bestD) { bestD = sq; best = n; }
                        }
                    }
                }
                if (best >= 0 && r >= 1) break;
            }
            return best;
        }

        internal int RandomNode(System.Random rng)
        {
            return Count == 0 ? -1 : rng.Next(Count);
        }
    }
}
