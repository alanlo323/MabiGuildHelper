using DiscordBot.Helper;

namespace DiscordBot
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LogHelper.CreateLogger();
            var logger = NLog.LogManager.GetCurrentClassLogger();
            string? token = ConfigHelper.GetValue("DISCORD_BOT_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.Info("Please enter DISCORD_BOT_TOKEN in config file.");
                Environment.Exit(1);
            }

            Bot bot = new(token);
            bot.Start();

            Thread.Sleep(-1);
        }
    }
}