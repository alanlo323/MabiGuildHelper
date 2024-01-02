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
    public class DailyDungeonInfoJob(ILogger<DailyDungeonInfoJob> logger, DiscordSocketClient client, AppDbContext appDbContext, ImgurHelper imgurHelper, DiscordApiHelper discordApiHelper) : IJob
    {
        public static readonly JobKey Key = new(nameof(DailyDungeonInfoJob));

        public async Task Execute(IJobExecutionContext context)
        {
            Embed embed = EmbedUtil.GetTodayDungeonInfoEmbed(imgurHelper, out DailyDungeonInfo todayDungeonInfo);
            var guildSettings = appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.DailyDungeonInfoChannelId), nameof(GuildSetting.DailyDungeonInfoMessageId), channelName: $"今日老手-{todayDungeonInfo.Name}", embed: embed);
            }
        }
    }
}
