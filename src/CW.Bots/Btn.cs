namespace CW.Bots
{
    internal static class Btn
    {
        internal const int Left = 1;
        internal const int Right = 2;
        internal const int Up = 4;
        internal const int Down = 8;
        internal const int Jump = 16;
        internal const int Sit = 32;
        internal const int Walk = 64;
        internal const int Interaction = 512;
        internal const int Knife = 1024;
        internal const int Secondary = 2048;
        internal const int Primary = 4096;
        internal const int Reload = 262144;
        internal const int Auto = 524288;
        internal const int Fire = 1048576;
        internal const int Aim = 2097152;
        internal const int Grenade = 4194304;

        private static readonly int[] Octants =
        {
            Up,
            Up | Right,
            Right,
            Down | Right,
            Down,
            Down | Left,
            Left,
            Up | Left
        };

        internal static int Move(float forward, float right)
        {
            if (forward * forward + right * right < 0.0004f) return 0;
            float deg = UnityEngine.Mathf.Atan2(right, forward) * UnityEngine.Mathf.Rad2Deg;
            int oct = UnityEngine.Mathf.RoundToInt(deg / 45f) & 7;
            return Octants[oct];
        }
    }
}
