using System.Collections.Generic;
using UnityEngine;

namespace CW.Bots.Nav
{
    internal sealed class NavQuery
    {
        internal int MaxExpansions = 6000;
        internal float HeuristicWeight = 1.15f;

        private readonly NavGrid _g;
        private readonly float[] _gScore;
        private readonly int[] _came;
        private readonly int[] _stamp;
        private readonly bool[] _closed;
        private readonly Heap _open;
        private int _run;

        internal int LastExpanded { get; private set; }

        internal NavQuery(NavGrid grid)
        {
            _g = grid;
            int n = grid.Count;
            _gScore = new float[n];
            _came = new int[n];
            _stamp = new int[n];
            _closed = new bool[n];
            _open = new Heap(n);
        }

        internal bool FindPath(int start, int goal, List<int> outPath)
        {
            outPath.Clear();
            if (start < 0 || goal < 0 || start >= _g.Count || goal >= _g.Count) return false;
            if (start == goal) { outPath.Add(start); return true; }

            _run++;
            _open.Clear();
            LastExpanded = 0;

            Touch(start);
            _gScore[start] = 0f;
            _came[start] = -1;
            _open.Push(start, Heuristic(start, goal));

            while (_open.Count > 0)
            {
                int cur = _open.Pop();
                if (_closed[cur]) continue;
                _closed[cur] = true;

                if (cur == goal) { Rebuild(goal, outPath); return true; }

                if (++LastExpanded > MaxExpansions) break;

                int s = _g.LinkStart[cur], e = _g.LinkStart[cur + 1];
                for (int i = s; i < e; i++)
                {
                    int nb = _g.LinkTo[i];
                    Touch(nb);
                    if (_closed[nb]) continue;

                    float step = Vector3.Distance(_g.Pos[cur], _g.Pos[nb]);
                    byte f = _g.LinkFlags[i];
                    if (f == LinkFlag.Jump) step += 1.5f;
                    else if (f == LinkFlag.Drop) step += 0.75f;

                    float tentative = _gScore[cur] + step;
                    if (tentative >= _gScore[nb]) continue;

                    _gScore[nb] = tentative;
                    _came[nb] = cur;
                    _open.Push(nb, tentative + Heuristic(nb, goal));
                }
            }
            return false;
        }

        private void Touch(int n)
        {
            if (_stamp[n] == _run) return;
            _stamp[n] = _run;
            _gScore[n] = float.MaxValue;
            _came[n] = -1;
            _closed[n] = false;
        }

        private float Heuristic(int a, int b)
        {
            return Vector3.Distance(_g.Pos[a], _g.Pos[b]) * HeuristicWeight;
        }

        private void Rebuild(int goal, List<int> outPath)
        {
            int cur = goal;
            while (cur >= 0) { outPath.Add(cur); cur = _came[cur]; }
            outPath.Reverse();
        }

        internal int StringPull(List<int> path, int from, Vector3 pos, float radius, float height, int mask)
        {
            int best = from;
            int limit = Mathf.Min(path.Count - 1, from + 6);
            for (int i = limit; i > from; i--)
            {
                if (DirectWalk(pos, _g.Pos[path[i]], radius, height, mask)) { best = i; break; }
            }
            return best;
        }

        internal static bool DirectWalk(Vector3 a, Vector3 b, float radius, float height, int mask)
        {
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float dist = flat.magnitude;
            if (dist < 0.05f) return true;
            if (b.y - a.y > 0.45f) return false;

            float r = radius - 0.06f;
            Vector3 feet = new Vector3(a.x, a.y + 0.4f, a.z);
            Vector3 p1 = feet + Vector3.up * r;
            Vector3 p2 = feet + Vector3.up * (height - r);
            return !Physics.CapsuleCast(p1, p2, r, flat / dist, dist, mask);
        }

        private sealed class Heap
        {
            private int[] _item;
            private float[] _key;
            private int _n;

            internal Heap(int cap) { cap = Mathf.Max(16, cap / 4); _item = new int[cap]; _key = new float[cap]; }
            internal int Count { get { return _n; } }
            internal void Clear() { _n = 0; }

            internal void Push(int item, float key)
            {
                if (_n == _item.Length) Grow();
                int i = _n++;
                _item[i] = item; _key[i] = key;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (_key[p] <= _key[i]) break;
                    Swap(p, i); i = p;
                }
            }

            internal int Pop()
            {
                int top = _item[0];
                _n--;
                if (_n > 0)
                {
                    _item[0] = _item[_n]; _key[0] = _key[_n];
                    int i = 0;
                    while (true)
                    {
                        int l = 2 * i + 1, r = l + 1, m = i;
                        if (l < _n && _key[l] < _key[m]) m = l;
                        if (r < _n && _key[r] < _key[m]) m = r;
                        if (m == i) break;
                        Swap(m, i); i = m;
                    }
                }
                return top;
            }

            private void Grow()
            {
                var ni = new int[_item.Length * 2];
                var nk = new float[_key.Length * 2];
                System.Array.Copy(_item, ni, _n);
                System.Array.Copy(_key, nk, _n);
                _item = ni; _key = nk;
            }

            private void Swap(int a, int b)
            {
                int ti = _item[a]; _item[a] = _item[b]; _item[b] = ti;
                float tk = _key[a]; _key[a] = _key[b]; _key[b] = tk;
            }
        }
    }
}
