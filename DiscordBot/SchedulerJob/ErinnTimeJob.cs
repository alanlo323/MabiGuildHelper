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
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Quartz;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SchedulerJob
{
    public class ErinnTimeJob : IJob
    {
        public static readonly JobKey Key = new("ErinnTime");

        ILogger<ErinnTimeJob> _logger;
        AppDbContext _appDbContext;
        DiscordApiHelper _discordApiHelper;

        public ErinnTimeJob(ILogger<ErinnTimeJob> logger, AppDbContext appDbContext, DiscordApiHelper discordApiHelper)
        {
            _logger = logger;
            _appDbContext = appDbContext;
            _discordApiHelper = discordApiHelper;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var guildSettings = _appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, "愛爾琳時間", nameof(GuildSetting.ErinnTimeChannelId), nameof(GuildSetting.ErinnTimeMessageId), null, EmbedUtil.GetErinnTimeEmbed(true));
            }
        }
    }
}
