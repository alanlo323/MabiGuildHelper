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
    public class DailyEffectJob : IJob
    {
        public static readonly JobKey Key = new("DailyEffectJob");

        ILogger<DailyEffectJob> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        GameConfig _gameConfig;
        DiscordApiHelper _discordApiHelper;

        public DailyEffectJob(ILogger<DailyEffectJob> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig, DiscordApiHelper discordApiHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _gameConfig = gameConfig.Value;
            _discordApiHelper = discordApiHelper;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var today = DateTime.Now.DayOfWeek.ToString();
            var todayEffect = _gameConfig.DailyEffect.First(x => x.DayOfWeek == today);
            string channelName = $"{todayEffect.ChannelName}";
            var guildSettings = _appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, channelName, nameof(GuildSetting.ErinnTimeChannelId), nameof(GuildSetting.ErinnTimeMessageId), null, EmbedUtil.GetDailyEffectEmbed(_gameConfig));
            }
        }
    }
}
