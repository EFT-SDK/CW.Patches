using UnityEngine;

namespace CW.Bots.Nav
{
    internal sealed class NavDebug : MonoBehaviour
    {
        internal static NavDebug Instance;
        internal bool Enabled;
        internal float DrawRange = 45f;

        private Material _mat;

        private const string ShaderSrc =
            "Shader \"CWBots/Lines\" {" +
            "SubShader { Tags { \"Queue\"=\"Overlay\" } Pass {" +
            "Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off ZTest Always" +
            "BindChannels { Bind \"Color\", color }" +
            "} } }";

        private void Awake()
        {
            Instance = this;
        }

        private void EnsureMaterial()
        {
            if (_mat != null) return;
            var sh = Shader.Find("CWBots/Lines");
            _mat = sh != null ? new Material(sh) : new Material(ShaderSrc);
            _mat.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnRenderObject()
        {
            if (!Enabled) return;
            var dir = BotDirector.Instance;
            if (dir == null || dir.Grid == null) return;

            var cam = Camera.current;
            if (cam == null) return;

            EnsureMaterial();
            _mat.SetPass(0);

            Vector3 eye = cam.transform.position;
            float rangeSq = DrawRange * DrawRange;
            var g = dir.Grid;

            GL.PushMatrix();
            GL.Begin(GL.LINES);

            for (int i = 0; i < g.Count; i++)
            {
                Vector3 a = g.Pos[i];
                if ((a - eye).sqrMagnitude > rangeSq) continue;

                int s = g.LinkStart[i], e = g.LinkStart[i + 1];
                for (int k = s; k < e; k++)
                {
                    int j = g.LinkTo[k];
                    if (j < i) continue;
                    byte f = g.LinkFlags[k];
                    if (f == LinkFlag.Jump) GL.Color(new Color(1f, 0.8f, 0.1f, 0.85f));
                    else if (f == LinkFlag.Drop) GL.Color(new Color(1f, 0.35f, 0.1f, 0.85f));
                    else GL.Color(new Color(0.25f, 0.9f, 0.4f, 0.55f));

                    GL.Vertex(a + Vector3.up * 0.08f);
                    GL.Vertex(g.Pos[j] + Vector3.up * 0.08f);
                }
            }

            for (int i = 0; i < g.Count; i++)
            {
                if (g.CoverHigh[i] == 0 && g.CoverLow[i] == 0) continue;
                Vector3 a = g.Pos[i];
                if ((a - eye).sqrMagnitude > rangeSq) continue;

                for (int d = 0; d < 8; d++)
                {
                    int bit = 1 << d;
                    bool high = (g.CoverHigh[i] & bit) != 0;
                    bool low = (g.CoverLow[i] & bit) != 0;
                    if (!high && !low) continue;

                    GL.Color(high ? new Color(0.2f, 0.5f, 1f, 0.8f) : new Color(0.6f, 0.6f, 0.2f, 0.6f));
                    Vector3 dir8 = new Vector3(NavGrid.DX[d], 0f, NavGrid.DZ[d]).normalized * 0.34f;
                    Vector3 baseAt = a + Vector3.up * (high ? 1.05f : 0.5f);
                    GL.Vertex(baseAt);
                    GL.Vertex(baseAt + dir8);
                }
            }

            GL.Color(new Color(0.3f, 0.7f, 1f, 0.95f));
            foreach (var agent in dir.Agents)
            {
                if (!agent.Alive || agent.Path.Count == 0) continue;
                for (int i = agent.PathIndex; i < agent.Path.Count - 1; i++)
                {
                    GL.Vertex(g.Pos[agent.Path[i]] + Vector3.up * 0.35f);
                    GL.Vertex(g.Pos[agent.Path[i + 1]] + Vector3.up * 0.35f);
                }
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}
