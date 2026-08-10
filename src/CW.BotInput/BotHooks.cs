namespace CW.BotInput
{
    internal static class BotHooks
    {
        internal static bool Mirror;

        public static int BotButtons(int saved)
        {
            return Mirror ? saved : 0;
        }
    }
}
