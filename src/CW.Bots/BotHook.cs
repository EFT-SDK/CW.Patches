namespace CW.Bots
{
    internal static class BotHook
    {
        public static int Think(int saved, object bot)
        {
            var dir = BotDirector.Instance;
            if (dir == null || !Refl.Ready) return 0;

            try
            {
                var agent = dir.Agent(bot);
                return agent == null ? 0 : agent.Think(dir);
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("bot think failed: " + e);
                return 0;
            }
        }
    }
}
