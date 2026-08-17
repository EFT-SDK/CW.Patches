using System.Collections;
using System.Collections.Generic;
using CW.Bots.Ai;
using CW.Bots.Nav;
using UnityEngine;

namespace CW.Bots
{
    internal sealed class BotDirector : MonoBehaviour
    {
        internal static BotDirector Instance;

        internal NavGrid Grid;
        internal NavQuery Query;
        internal float TurnRate = 420f;
        internal float Radius = 0.4f;
        internal float Height = 1.74f;
        internal int Mask;

        internal int Wanted;
        internal bool ShowDebug;
        internal int SkillTier = 1;
        internal readonly Blackboard Blackboard = new Blackboard();
        internal object CachedGame { get; private set; }
        internal bool IsTeamGame { get; private set; }

        internal Difficulty Skill { get { return Difficulty.Get(SkillTier); } }

        private readonly Dictionary<int, BotAgent> _agents = new Dictionary<int, BotAgent>();
        private readonly System.Random _rng = new System.Random(20260810);
        private NavBaker _baker;
        private string _bakedMap = string.Empty;
        private string _lastMap = string.Empty;
        private float _nextTopUp;

        internal int AgentCount { get { return _agents.Count; } }
        internal bool Baking { get { return _baker != null && _baker.Running; } }
        internal string BakeStatus { get { return _baker == null ? "idle" : _baker.Status; } }
        internal IEnumerable<BotAgent> Agents { get { return _agents.Values; } }

        private void Awake()
        {
            Instance = this;
        }

        internal BotAgent Agent(object bot)
        {
            if (bot == null) return null;
            int group;
            try { group = Refl.Group(bot); }
            catch { return null; }

            BotAgent a;
            if (!_agents.TryGetValue(group, out a))
            {
                a = new BotAgent { Group = group, Bot = bot };
                _agents[group] = a;
            }
            a.Bot = bot;
            return a;
        }

        private void Update()
        {
            if (!Refl.Ready) return;

            bool hosting = false;
            object game = null;
            try
            {
                hosting = Refl.IsServer && Refl.IsGameLoaded;
                if (hosting) game = Refl.ServerGame;
            }
            catch { return; }

            if (!hosting || game == null)
            {
                if (_agents.Count > 0) _agents.Clear();
                CachedGame = null;
                _lastMap = string.Empty;
                return;
            }

            CachedGame = game;
            IsTeamGame = Refl.IsTeamGame(game);

            string map = Refl.MapName;
            if (map != _lastMap)
            {
                _lastMap = map;
                OnMapChanged(map);
            }

            PruneAgents();
            BindBodies(game);

            if (Wanted > 0 && Time.time >= _nextTopUp)
            {
                _nextTopUp = Time.time + 2f;
                TopUp(game);
            }
        }

        private void OnMapChanged(string map)
        {
            _agents.Clear();
            _botBodies.Clear();
            _kits.Clear();
            Blackboard.Clear();
            Grid = null;
            Query = null;
            _bakedMap = string.Empty;
            Mask = Refl.LevelLayers;

            if (string.IsNullOrEmpty(map)) return;

            var cached = NavCache.Load(map);
            if (cached != null)
            {
                Adopt(cached);
                Plugin.Log.LogInfo("nav loaded from cache for " + map + ": " + cached.Count + " nodes");
                return;
            }

            if (Plugin.AutoBake.Value) StartBake(map, false);
        }

        internal void Adopt(NavGrid grid)
        {
            Grid = grid;
            Query = new NavQuery(grid);
            _bakedMap = grid.Map;
            foreach (var a in _agents.Values) { a.Destination = -1; a.ClearPath(); }
        }

        internal bool StartBake(string map, bool force)
        {
            if (Baking) return false;
            if (string.IsNullOrEmpty(map)) return false;
            if (!force && _bakedMap == map && Grid != null) return false;

            _baker = new NavBaker
            {
                Cell = Plugin.NavCell.Value,
                FrameBudgetMs = Plugin.BakeBudgetMs.Value
            };
            StartCoroutine(BakeRoutine(map));
            return true;
        }

        private IEnumerator BakeRoutine(string map)
        {
            Plugin.Log.LogInfo("nav bake starting for " + map);
            float t0 = Time.realtimeSinceStartup;

            yield return StartCoroutine(_baker.Bake(map));

            if (_baker.Result != null)
            {
                Radius = _baker.Radius;
                Height = _baker.Height;
                Adopt(_baker.Result);
                NavCache.Save(_baker.Result);
                float secs = Time.realtimeSinceStartup - t0;
                string msg = "nav bake done for " + map + ": " + _baker.Result.Count + " nodes, "
                             + _baker.Result.LinkCount + " links in " + secs.ToString("F1") + "s";
                Plugin.Log.LogInfo(msg);
                Refl.Print(msg);
            }
            else
            {
                Plugin.Log.LogWarning("nav bake failed for " + map + ": " + _baker.Status);
                Refl.Print("nav bake failed: " + _baker.Status);
            }
        }

        private readonly HashSet<int> _botBodies = new HashSet<int>();
        private readonly Dictionary<int, Kit> _kits = new Dictionary<int, Kit>();

        internal void RegisterBotBody(int group) { _botBodies.Add(group); }
        internal bool IsBotBody(int group) { return _botBodies.Contains(group); }

        internal Kit KitFor(int group, object userInfo)
        {
            Kit kit;
            if (_kits.TryGetValue(group, out kit)) return kit;

            kit = Loadout.Roll(userInfo, KitSeed + group * 7919, Plugin.SkillCount.Value, Plugin.Grenades.Value);
            _kits[group] = kit;
            return kit;
        }

        internal BotAgent AgentByGroup(int group)
        {
            BotAgent a;
            return _agents.TryGetValue(group, out a) ? a : null;
        }

        internal void ReportGunshot(object shooter, Vector3 at, float radius)
        {
            if (_agents.Count == 0) return;
            float sq = radius * radius;

            foreach (var a in _agents.Values)
            {
                if (!a.Alive || ReferenceEquals(a.Server, shooter)) continue;
                float d2 = (a.Position - at).sqrMagnitude;
                if (d2 > sq) continue;
                a.Hearing.OnNoise(at, 1f - Mathf.Sqrt(d2) / radius);
            }
        }

        internal Kit PeekKit(int group)
        {
            Kit kit;
            return _kits.TryGetValue(group, out kit) ? kit : null;
        }

        internal void RerollKits()
        {
            KitSeed = _rng.Next(int.MaxValue);
            _kits.Clear();
        }

        internal int KitSeed = 1337;

        private readonly List<int> _dead = new List<int>();

        private void PruneAgents()
        {
            if (_agents.Count == 0) return;

            _dead.Clear();
            foreach (var kv in _agents)
            {
                var a = kv.Value;
                if (a.LastTick > 0f && Time.time - a.LastTick > 3f) _dead.Add(kv.Key);
            }
            for (int i = 0; i < _dead.Count; i++) _agents.Remove(_dead[i]);
        }

        private void BindBodies(object game)
        {
            if (_agents.Count == 0) return;

            var list = Refl.ServerPlayers(game);
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var sp = list[i];
                if (sp == null) continue;
                int g;
                try { g = Refl.Group(sp); }
                catch { continue; }

                BotAgent a;
                if (_agents.TryGetValue(g, out a)) a.BindServer(sp);
            }
        }

        private void TopUp(object game)
        {
            var list = Refl.ServerPlayers(game);
            if (list == null) return;

            int bots = _agents.Count;
            int missing = Wanted - bots;
            if (missing <= 0) return;
            if (missing > 4) missing = 4;

            Refl.AddBot(missing);
        }

        private int _bearCount, _usecCount, _tallyFrame = -1;

        private void RecountTeams(object game)
        {
            if (_tallyFrame == Time.frameCount) return;
            _tallyFrame = Time.frameCount;
            _bearCount = 0;
            _usecCount = 0;

            var list = Refl.ServerPlayers(game);
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var info = Refl.PlayerInfo(list[i]);
                if (info == null) continue;
                int t = Refl.PlayerType(info);
                if (t == 0) _bearCount++;
                else if (t == 1) _usecCount++;
            }
        }

        internal int ClaimTeam(object game)
        {
            RecountTeams(game);
            if (_bearCount <= _usecCount) { _bearCount++; return 0; }
            _usecCount++;
            return 1;
        }

        internal int PickRoamTarget(NavGrid grid, Vector3 from)
        {
            if (grid.Count == 0) return -1;

            int best = -1;
            float bestScore = float.MinValue;
            for (int i = 0; i < 24; i++)
            {
                int n = grid.RandomNode(_rng);
                if (n < 0) continue;
                float d = Vector3.Distance(grid.Pos[n], from);
                if (d < 12f) continue;

                float score = -Mathf.Abs(d - 55f) + grid.Enclosure(n) * 1.5f + (float)_rng.NextDouble() * 8f;
                if (score > bestScore) { bestScore = score; best = n; }
            }
            return best >= 0 ? best : grid.RandomNode(_rng);
        }

        internal void Clear()
        {
            Wanted = 0;
            _agents.Clear();
        }

        private void OnGUI()
        {
            if (!ShowDebug) return;

            var style = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            GUI.color = Color.white;

            float y = 4f;
            GUI.Label(new Rect(6f, y, 900f, 18f),
                "CW.Bots  map=" + _lastMap + "  nav=" + (Grid == null ? "none" : Grid.Count + " nodes")
                + "  bake=" + BakeStatus + "  agents=" + _agents.Count + "/" + Wanted, style);
            y += 15f;

            foreach (var a in _agents.Values)
            {
                GUI.Label(new Rect(6f, y, 900f, 18f),
                    "  g" + a.Group + " team=" + a.Team + " alive=" + (a.Alive ? 1 : 0)
                    + " " + a.State + " dest=" + a.Destination, style);
                y += 14f;
                if (y > Screen.height - 20f) break;
            }
        }
    }
}
