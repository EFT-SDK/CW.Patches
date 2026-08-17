using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CW.Bots.Nav;
using HarmonyLib;
using UnityEngine;

namespace CW.Bots
{
    [BepInPlugin(Guid, "CW Bots", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string Guid = "com.cw.bots";
        internal static ManualLogSource Log;

        internal static ConfigEntry<bool> AutoBake;
        internal static ConfigEntry<float> NavCell;
        internal static ConfigEntry<float> BakeBudgetMs;
        internal static ConfigEntry<float> TurnRate;
        internal static ConfigEntry<int> Skill;
        internal static ConfigEntry<bool> RandomKits;
        internal static ConfigEntry<int> SkillCount;
        internal static ConfigEntry<bool> Grenades;
        internal static ConfigEntry<bool> GuardEntities;
        internal static ConfigEntry<bool> UseGrenades;
        internal static ConfigEntry<bool> UseCover;

        private void Awake()
        {
            Log = Logger;

            AutoBake = Config.Bind("nav", "AutoBake", true,
                "Bake a navigation grid automatically the first time a map is hosted.");
            NavCell = Config.Bind("nav", "Cell", 0.75f,
                "Grid spacing in metres. Smaller is more accurate and much slower to bake.");
            BakeBudgetMs = Config.Bind("nav", "BakeBudgetMs", 3f,
                "Milliseconds of bake work per frame. Higher bakes faster and stutters more.");
            TurnRate = Config.Bind("bots", "TurnRate", 420f,
                "Fallback view turn rate in degrees per second, used before a skill tier applies.");
            Skill = Config.Bind("bots", "Skill", 1,
                "Difficulty tier: 0 recruit, 1 regular, 2 veteran, 3 elite. Change live with 'botskill'.");

            RandomKits = Config.Bind("loadout", "RandomKits", true,
                "Give each bot its own randomly rolled weapons and skills instead of copying the host's suit.");
            SkillCount = Config.Bind("loadout", "SkillCount", 14,
                "How many skills each bot rolls, on top of the grenade line. Prerequisites unlock automatically.");
            Grenades = Config.Bind("loadout", "Grenades", true,
                "Always roll the grenade skill line (efd, efd2, efd_throw, ...) so bots carry grenades.");

            UseGrenades = Config.Bind("combat", "UseGrenades", true,
                "Bots throw grenades, solving a real ballistic arc for the target.");
            UseCover = Config.Bind("combat", "UseCover", true,
                "Bots break to cover when reloading, hurt or outgunned, and crouch behind low cover.");

            GuardEntities = Config.Bind("safety", "GuardEntities", true,
                "Stop one broken player entity from aborting the whole client render loop. "
                + "BaseClientGame.OnLateUpdate iterates entities with a plain foreach, so an exception "
                + "in one freezes every entity after it in the list.");

            Refl.Init();
            if (!Refl.Ready)
            {
                Log.LogError("reflection bind failed; CW.Bots is inert");
                return;
            }

            var go = new GameObject("CW.Bots");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;

            var dir = go.AddComponent<BotDirector>();
            dir.TurnRate = TurnRate.Value;
            dir.SkillTier = Skill.Value;
            go.AddComponent<NavDebug>();

            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo("CW.Bots ready - console: bots, botskill, navbake, navinfo, navdraw, botdebug");
        }
    }
}
