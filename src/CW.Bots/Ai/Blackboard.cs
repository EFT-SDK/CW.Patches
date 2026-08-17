using UnityEngine;

namespace CW.Bots.Ai
{
    internal sealed class Blackboard
    {
        private readonly Vector3[] _pos = new Vector3[2];
        private readonly float[] _at = new float[2];

        internal void Report(int team, Vector3 enemyAt)
        {
            if (team < 0 || team > 1) return;
            _pos[team] = enemyAt;
            _at[team] = Time.time;
        }

        internal bool Contact(int team, float within, out Vector3 enemyAt)
        {
            enemyAt = Vector3.zero;
            if (team < 0 || team > 1) return false;
            if (_at[team] <= 0f || Time.time - _at[team] > within) return false;
            enemyAt = _pos[team];
            return true;
        }

        internal void Clear()
        {
            _at[0] = 0f;
            _at[1] = 0f;
        }
    }
}
