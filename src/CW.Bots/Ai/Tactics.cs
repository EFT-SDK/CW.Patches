using System.Collections.Generic;
using CW.Bots.Nav;
using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Tactics
    {
        internal int CoverNode = -1;
        internal int CoverKindHeld = CoverKind.None;
        internal float CoverPickedAt = -1f;

        private readonly List<int> _scratch = new List<int>();

        internal void Forget()
        {
            CoverNode = -1;
            CoverKindHeld = CoverKind.None;
            CoverPickedAt = -1f;
        }

        internal bool Holding(NavGrid grid, Vector3 self, out Vector3 at)
        {
            at = Vector3.zero;
            if (CoverNode < 0 || grid == null || CoverNode >= grid.Count) return false;
            at = grid.Pos[CoverNode];
            return true;
        }

        internal bool AtCover(NavGrid grid, Vector3 self)
        {
            Vector3 at;
            if (!Holding(grid, self, out at)) return false;
            Vector3 d = at - self;
            d.y = 0f;
            return d.sqrMagnitude < 1.4f * 1.4f;
        }

        internal int Find(NavGrid grid, Vector3 self, Vector3 threat, Doctrine style, float searchRadius)
        {
            if (grid == null || grid.Count == 0) return -1;

            grid.Around(self, searchRadius, _scratch);
            if (_scratch.Count == 0) return -1;

            int best = -1;
            float bestScore = float.MinValue;

            for (int i = 0; i < _scratch.Count; i++)
            {
                int n = _scratch[i];
                int kind = grid.CoverAgainst(n, threat);
                if (kind == CoverKind.None) continue;

                Vector3 p = grid.Pos[n];
                float toThreat = Vector3.Distance(p, threat);
                float toSelf = Vector3.Distance(p, self);

                float score = kind == CoverKind.Full ? 26f : 16f;
                score -= toSelf * 1.4f;
                score -= Mathf.Abs(toThreat - Mathf.Lerp(style.IdealMin, style.IdealMax, 0.5f)) * 0.5f;

                if (toThreat < style.IdealMin * 0.5f) score -= 14f;
                if (grid.CoverAgainst(n, threat) == CoverKind.Full && CanPeek(grid, n, threat)) score += 10f;

                if (score > bestScore) { bestScore = score; best = n; }
            }

            return best;
        }

        internal bool Take(NavGrid grid, Vector3 self, Vector3 threat, Doctrine style, float searchRadius)
        {
            int node = Find(grid, self, threat, style, searchRadius);
            if (node < 0) return false;

            CoverNode = node;
            CoverKindHeld = grid.CoverAgainst(node, threat);
            CoverPickedAt = Time.time;
            return true;
        }

        private static bool CanPeek(NavGrid grid, int node, Vector3 threat)
        {
            Vector3 p = grid.Pos[node];
            Vector3 toThreat = threat - p;
            toThreat.y = 0f;
            if (toThreat.sqrMagnitude < 0.04f) return false;
            toThreat.Normalize();

            Vector3 side = new Vector3(-toThreat.z, 0f, toThreat.x) * 1.1f;
            Vector3 eye = p + Vector3.up * 1.4f;

            return !Physics.Linecast(eye + side, threat, BotDirector.Instance.Mask)
                   || !Physics.Linecast(eye - side, threat, BotDirector.Instance.Mask);
        }

        internal Vector3 PeekOffset(Vector3 coverPos, Vector3 threat, int mask, out bool found)
        {
            found = false;
            Vector3 toThreat = threat - coverPos;
            toThreat.y = 0f;
            if (toThreat.sqrMagnitude < 0.04f) return Vector3.zero;
            toThreat.Normalize();

            Vector3 side = new Vector3(-toThreat.z, 0f, toThreat.x);
            Vector3 eye = coverPos + Vector3.up * 1.4f;

            if (!Physics.Linecast(eye + side * 1.1f, threat, mask)) { found = true; return side; }
            if (!Physics.Linecast(eye - side * 1.1f, threat, mask)) { found = true; return -side; }
            return Vector3.zero;
        }
    }
}
