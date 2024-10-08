﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Quartz;
using static DiscordBot.SemanticKernel.SemanticKernelEngine;

namespace DiscordBot.SchedulerJob
{
    public class DataScrapingJob(ILogger<DataScrapingJob> logger, AppDbContext appDbContext, DiscordApiHelper discordApiHelper, DataScrapingHelper dataScrapingHelper, SemanticKernelEngine semanticKernelEngine) : IJob
    {
        public static readonly JobKey Key = new(nameof(DataScrapingJob));

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Checking news");

            var dataScrapingResult = await dataScrapingHelper.GetMabinogiNews();
            var guildSettings = appDbContext.GuildSettings.ToList();

            foreach (News news in dataScrapingResult.UpdatedNews.Concat(dataScrapingResult.NewNews))
            {
                var kernel = await semanticKernelEngine.GetKernelAsync(Usage.DataScrapingJob);
                string summary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "SummarizeMabiNewsHtml", arguments: new()
                {
                    { "input", news.HtmlContent },
                    { "kernel", kernel },
                });
                news.AiContent = summary;
                await appDbContext.SaveChangesAsync();

                Embed embed = EmbedUtil.GetMainogiNewsEmbed(news);
                foreach (GuildSetting guildSetting in guildSettings)
                {
                    if (!guildSetting.DataScapingNewsChannelId.HasValue) continue;

                    await discordApiHelper.SendFile(guildSetting.GuildId, guildSetting.DataScapingNewsChannelId, news.SnapshotTempFile.FullName, embed: embed);
                }
            }
        }
    }
}
