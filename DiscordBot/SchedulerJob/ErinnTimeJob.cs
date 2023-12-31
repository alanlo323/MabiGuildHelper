﻿using System;
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
    public class ErinnTimeJob(ILogger<ErinnTimeJob> logger, AppDbContext appDbContext, DiscordApiHelper discordApiHelper) : IJob
    {
        public static readonly JobKey Key = new(nameof(ErinnTimeJob));

        public async Task Execute(IJobExecutionContext context)
        {
            var guildSettings = appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.ErinnTimeChannelId), nameof(GuildSetting.ErinnTimeMessageId), channelName: "愛爾琳時間", embed: EmbedUtil.GetErinnTimeEmbed(true));
            }
        }
    }
}
