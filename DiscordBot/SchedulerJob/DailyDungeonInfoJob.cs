using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.DataEntity;
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
    public class DailyDungeonInfoJob : IJob
    {
        public static readonly JobKey Key = new(nameof(DailyDungeonInfoJob));

        ILogger<DailyDungeonInfoJob> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        ImgurHelper _imgurHelper;
        DiscordApiHelper _discordApiHelper;

        public DailyDungeonInfoJob(ILogger<DailyDungeonInfoJob> logger, DiscordSocketClient client, AppDbContext appDbContext, ImgurHelper imgurHelper, DiscordApiHelper discordApiHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _imgurHelper = imgurHelper;
            _discordApiHelper = discordApiHelper;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Embed embed = EmbedUtil.GetTodayDungeonInfoEmbed(_imgurHelper, out DailyDungeonInfo todayDungeonInfo);
            var guildSettings = _appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.DailyDungeonInfoChannelId), nameof(GuildSetting.DailyDungeonInfoMessageId), channelName: $"今日老手-{todayDungeonInfo.Name}", embed: embed);
            }
        }
    }
}
