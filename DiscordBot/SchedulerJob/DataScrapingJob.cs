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
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Migrations;
using DiscordBot.SemanticKernel;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Quartz;
using static DiscordBot.SemanticKernel.SemanticKernelEngine;
using News = DiscordBot.Db.Entity.News;

namespace DiscordBot.SchedulerJob
{
    public class DataScrapingJob(ILogger<DataScrapingJob> logger, DiscordSocketClient client, AppDbContext appDbContext, DiscordApiHelper discordApiHelper, DataScrapingHelper dataScrapingHelper, SemanticKernelEngine semanticKernelEngine) : IJob
    {
        public static readonly JobKey Key = new(nameof(DataScrapingJob));

        public async Task Execute(IJobExecutionContext context)
        {
            logger.LogInformation("Checking news");

            var dataScrapingResult = await dataScrapingHelper.GetMabinogiNews();
            var guildSettings = appDbContext.GuildSettings.ToList();
            var newsList = dataScrapingResult.UpdatedNews.Concat(dataScrapingResult.NewNews);

            // Generate Ai Content
            foreach (News news in newsList)
            {
                var kernel = await semanticKernelEngine.GetKernelAsync(Scope.DataScrapingJob);
                string summary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "SummarizeMabiNewsHtml", arguments: new()
                {
                    { "input", news.HtmlContent },
                    { "kernel", kernel },
                });
                news.AiContent = summary;
                await appDbContext.SaveChangesAsync();
            }

            // Post news
            foreach (News news in newsList)
            {

                Embed embed = EmbedUtil.GetMainogiNewsEmbed(news);
                foreach (GuildSetting guildSetting in guildSettings)
                {
                    try
                    {
                        if (!guildSetting.DataScapingNewsChannelId.HasValue) continue;
                        logger.LogInformation($"Posting news: {news.Title} to {guildSetting.GuildId}");

                        // Post to News Channel
                        var postResult = await discordApiHelper.SendFile(guildSetting.GuildId, guildSetting.DataScapingNewsChannelId, news.SnapshotTempFile.FullName, embed: embed);
                        if (postResult.Item1.GetChannelType() == ChannelType.News) await postResult.Item2!.CrosspostAsync();

                        Thread newThread = new(async () =>
                        {
                            try
                            {
                                // Create or Update Event
                                if (guildSetting.GuildId == 1058732396998033428)    // Temporarily hardcode
                                {
                                    var response = await semanticKernelEngine.GenerateResponse(Scope.DataScrapingJob, news.HtmlContent, metaData: new { guildSetting.GuildId });
                                    await discordApiHelper.LogMessage(guildSetting.GuildId, response.Conversation.Result);
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogException(ex);
                            }
                        });
                        newThread.Start();
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex);
                    }
                }
            }
        }
    }
}
