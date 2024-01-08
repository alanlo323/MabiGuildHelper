using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SchedulerJob
{
    public class DailyEffectJob(ILogger<DailyEffectJob> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig, DiscordApiHelper discordApiHelper) : IJob
    {
        public static readonly JobKey Key = new(nameof(DailyEffectJob));
        GameConfig _gameConfig = gameConfig.Value;

        public async Task Execute(IJobExecutionContext context)
        {
            var today = DateTime.Now.DayOfWeek.ToString();
            var todayEffect = _gameConfig.DailyEffect.First(x => x.DayOfWeek == today);
            string channelName = $"{todayEffect.ChannelName}";
            var guildSettings = appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.DailyEffectChannelId), nameof(GuildSetting.DailyEffectMessageId), channelName: channelName, embed: EmbedUtil.GetDailyEffectEmbed(_gameConfig));
            }
        }
    }
}
