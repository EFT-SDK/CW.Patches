using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CW.Bots.Nav
{
    internal sealed class NavBaker
    {
        internal const int Version = 2;

        internal float Cell = 0.75f;
        internal float Radius = 0.4f;
        internal float Height = 1.74f;
        internal float StepUp = 0.35f;
        internal float JumpUp = 1.1f;
        internal float MaxDrop = 3.4f;
        internal float SlopeCos = 0.643f;
        internal float CoverLowHeight = 0.55f;
        internal float CoverHighHeight = 1.35f;
        internal float CoverProbe = 1.3f;
        internal int MaxNodes = 60000;
        internal float FrameBudgetMs = 3f;

        internal bool Running { get; private set; }
        internal string Status { get; private set; }
        internal NavGrid Result { get; private set; }

        private int _mask;
        private Vector3 _min, _max;

        private readonly List<Vector3> _pos = new List<Vector3>();
        private readonly List<List<int>> _to = new List<List<int>>();
        private readonly List<List<byte>> _flags = new List<List<byte>>();
        private readonly Dictionary<long, int> _index = new Dictionary<long, int>();
        private readonly Queue<int> _frontier = new Queue<int>();


        internal IEnumerator Bake(string map)
        {
            Running = true;
            Status = "starting";
            Result = null;
            _pos.Clear(); _to.Clear(); _flags.Clear(); _index.Clear(); _frontier.Clear();
            _coverLow.Clear(); _coverHigh.Clear();

            _mask = Refl.LevelLayers;
            AdoptPlayerCapsule();

            if (!ResolveBounds())
            {
                Status = "no bounds markers on this map";
                Running = false;
                yield break;
            }

            var seeds = CollectSeeds();
            if (seeds.Count == 0)
            {
                Status = "no spawn points found";
                Running = false;
                yield break;
            }

            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < seeds.Count; i++)
            {
                Vector3 g;
                if (GroundAt(seeds[i] + Vector3.up * 1.5f, 6f, out g)) AddNode(g);
            }

            if (_pos.Count == 0)
            {
                Status = "no seed landed on walkable ground";
                Running = false;
                yield break;
            }

            Status = "flood fill";
            while (_frontier.Count > 0)
            {
                if (sw.Elapsed.TotalMilliseconds > FrameBudgetMs)
                {
                    Status = "flood fill: " + _pos.Count + " nodes";
                    yield return null;
                    sw.Reset(); sw.Start();
                }

                int cur = _frontier.Dequeue();
                Expand(cur);

                if (_pos.Count >= MaxNodes)
                {
                    _frontier.Clear();
                    Status = "node cap " + MaxNodes + " hit, stopping early";
                    Plugin.Log.LogWarning("nav bake hit the " + MaxNodes + " node cap on " + map + "; raise MaxNodes or Cell");
                    break;
                }
            }

            Status = "cover pass";
            for (int i = 0; i < _pos.Count; i++)
            {
                if (sw.Elapsed.TotalMilliseconds > FrameBudgetMs)
                {
                    Status = "cover pass: " + i + "/" + _pos.Count;
                    yield return null;
                    sw.Reset(); sw.Start();
                }
                CoverAt(i);
            }

            Result = Compact(map);
            Status = "done: " + Result.Count + " nodes, " + Result.LinkCount + " links";
            Running = false;
        }

        private void AdoptPlayerCapsule()
        {
            var cc = LiveCapsule();
            if (cc == null) return;
            Radius = cc.radius;
            Height = cc.height;
            StepUp = cc.stepOffset;
            SlopeCos = Mathf.Cos(cc.slopeLimit * Mathf.Deg2Rad);
        }

        private static CharacterController LiveCapsule()
        {
            var game = Refl.ServerGame;
            if (game == null) return null;
            var list = Refl.ServerPlayers(game);
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var mc = Refl.Controller(list[i]);
                if (mc == null) continue;
                var cc = Refl.CharController(mc);
                if (cc != null) return cc;
            }
            return null;
        }

        private bool ResolveBounds()
        {
            var ul = Object.FindObjectOfType(Refl.TUpperLeftPoint) as Component;
            var lr = Object.FindObjectOfType(Refl.TLowerRightPoint) as Component;
            if (ul == null || lr == null) return false;

            Vector3 a = ul.transform.position, b = lr.transform.position;
            _min = new Vector3(Mathf.Min(a.x, b.x) - 4f, -500f, Mathf.Min(a.z, b.z) - 4f);
            _max = new Vector3(Mathf.Max(a.x, b.x) + 4f, 500f, Mathf.Max(a.z, b.z) + 4f);
            return true;
        }

        private List<Vector3> CollectSeeds()
        {
            var seeds = new List<Vector3>();
            var sps = Object.FindObjectsOfType(Refl.TSpawnPoint);
            for (int i = 0; i < sps.Length; i++)
            {
                var c = sps[i] as Component;
                if (c != null) seeds.Add(c.transform.position);
            }
            var game = Refl.ServerGame;
            if (game != null)
            {
                var list = Refl.ServerPlayers(game);
                if (list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var mc = Refl.Controller(list[i]);
                        if (mc == null) continue;
                        var cc = Refl.CharController(mc);
                        if (cc != null) seeds.Add(cc.transform.position);
                    }
                }
            }
            return seeds;
        }

        private bool GroundAt(Vector3 from, float dist, out Vector3 ground)
        {
            ground = Vector3.zero;
            RaycastHit hit;
            if (!Physics.Raycast(from, Vector3.down, out hit, dist, _mask)) return false;
            if (hit.normal.y < SlopeCos) return false;
            ground = hit.point;
            return true;
        }

        private bool Fits(Vector3 ground)
        {
            float r = Radius - 0.02f;
            Vector3 feet = ground + Vector3.up * 0.06f;
            Vector3 p1 = feet + Vector3.up * r;
            Vector3 p2 = feet + Vector3.up * (Height - r);
            return !Physics.CheckCapsule(p1, p2, r, _mask);
        }

        private long Key(Vector3 p)
        {
            long ix = Mathf.RoundToInt(p.x / Cell);
            long iz = Mathf.RoundToInt(p.z / Cell);
            long iy = Mathf.RoundToInt(p.y / 1.0f);
            return ((ix & 0xFFFFF) << 40) | ((iz & 0xFFFFF) << 20) | (iy & 0xFFFFF);
        }

        private int AddNode(Vector3 ground)
        {
            if (ground.x < _min.x || ground.x > _max.x || ground.z < _min.z || ground.z > _max.z) return -1;
            long k = Key(ground);
            int existing;
            if (_index.TryGetValue(k, out existing)) return existing;
            if (!Fits(ground)) return -1;

            int id = _pos.Count;
            _pos.Add(ground);
            _to.Add(new List<int>(8));
            _flags.Add(new List<byte>(8));
            _index[k] = id;
            _frontier.Enqueue(id);
            return id;
        }

        private void Expand(int id)
        {
            Vector3 a = _pos[id];
            for (int d = 0; d < 8; d++)
            {
                Vector3 probe = a + new Vector3(NavGrid.DX[d] * Cell, 0f, NavGrid.DZ[d] * Cell);
                Vector3 from = new Vector3(probe.x, a.y + JumpUp + 0.1f, probe.z);

                Vector3 g;
                if (!GroundAt(from, JumpUp + 0.2f + MaxDrop, out g)) continue;

                float dy = g.y - a.y;
                if (dy > JumpUp || dy < -MaxDrop) continue;

                int nb = AddNode(g);
                if (nb < 0 || nb == id) continue;
                if (Linked(id, nb)) continue;

                byte flag;
                if (!Reachable(a, g, dy, out flag)) continue;

                _to[id].Add(nb);
                _flags[id].Add(flag);
            }
        }

        private bool Linked(int a, int b)
        {
            var list = _to[a];
            for (int i = 0; i < list.Count; i++) if (list[i] == b) return true;
            return false;
        }

        private bool Reachable(Vector3 a, Vector3 b, float dy, out byte flag)
        {
            flag = LinkFlag.Walk;
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float dist = flat.magnitude;
            if (dist < 0.001f) return false;
            Vector3 dir = flat / dist;

            if (Clear(a, dir, dist, StepUp)) { flag = dy < -StepUp ? LinkFlag.Drop : LinkFlag.Walk; return true; }
            if (dy <= JumpUp && Clear(a, dir, dist, JumpUp)) { flag = LinkFlag.Jump; return true; }
            return false;
        }

        private bool Clear(Vector3 ground, Vector3 dir, float dist, float lift)
        {
            float r = Radius - 0.04f;
            Vector3 feet = ground + Vector3.up * (lift + 0.06f);
            Vector3 p1 = feet + Vector3.up * r;
            Vector3 p2 = feet + Vector3.up * (Height - r);
            return !Physics.CapsuleCast(p1, p2, r, dir, dist, _mask);
        }

        private void CoverAt(int id)
        {
            byte low = 0, high = 0;
            Vector3 at = _pos[id];

            for (int d = 0; d < 8; d++)
            {
                Vector3 dir = new Vector3(NavGrid.DX[d], 0f, NavGrid.DZ[d]).normalized;
                if (Physics.Raycast(at + Vector3.up * CoverLowHeight, dir, CoverProbe, _mask)) low |= (byte)(1 << d);
                if (Physics.Raycast(at + Vector3.up * CoverHighHeight, dir, CoverProbe, _mask)) high |= (byte)(1 << d);
            }

            while (_coverLow.Count <= id) { _coverLow.Add(0); _coverHigh.Add(0); }
            _coverLow[id] = low;
            _coverHigh[id] = high;
        }

        private readonly List<byte> _coverLow = new List<byte>();
        private readonly List<byte> _coverHigh = new List<byte>();

        private NavGrid Compact(string map)
        {
            int n = _pos.Count;
            var grid = new NavGrid
            {
                Map = map,
                Cell = Cell,
                Min = _min,
                Max = _max,
                Pos = _pos.ToArray(),
                CoverLow = new byte[n],
                CoverHigh = new byte[n],
                LinkStart = new int[n + 1]
            };

            for (int i = 0; i < n; i++)
            {
                grid.CoverLow[i] = i < _coverLow.Count ? _coverLow[i] : (byte)0;
                grid.CoverHigh[i] = i < _coverHigh.Count ? _coverHigh[i] : (byte)0;
            }

            int total = 0;
            for (int i = 0; i < n; i++) { grid.LinkStart[i] = total; total += _to[i].Count; }
            grid.LinkStart[n] = total;

            grid.LinkTo = new int[total];
            grid.LinkFlags = new byte[total];
            int w = 0;
            for (int i = 0; i < n; i++)
            {
                var t = _to[i];
                var f = _flags[i];
                for (int j = 0; j < t.Count; j++) { grid.LinkTo[w] = t[j]; grid.LinkFlags[w] = f[j]; w++; }
            }

            grid.BuildIndex();
            return grid;
        }
    }
}
