using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class DebugCommand(ILogger<DebugCommand> logger, AppDbContext appDbContext, DiscordApiHelper discordApiHelper, IOptionsSnapshot<DiscordBotConfig> discordBotConfig) : IBaseSlashCommand
    {
        public string Name { get; set; } = "debug";
        public string Description { get; set; } = "測試";
        public CommandAvailability Availability { get; set; } = CommandAvailability.AdminServerOnly;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            ulong[] allowedUser = [ulong.Parse(discordBotConfig.Value.AdminId)];
            if (!allowedUser.Contains(command.User.Id))
            {
                await command.RespondAsync("你沒有權限使用此指令", ephemeral: true);
                return;
            }

            await command.DeferAsync();
            try
            {
                var sameKeyNews = appDbContext.News.Skip(1).Take(1).ToList();
                MabinogiNewsResult dataScrapingResult = new()
                {
                    NewNews = sameKeyNews,
                    UpdatedNews = [],
                    LoadedNews = [],
                };

                var guildSettings = appDbContext.GuildSettings.ToList();

                foreach (GuildSetting guildSetting in guildSettings)
                {
                    if (!guildSetting.DataScapingNewsChannelId.HasValue) continue;
                    if (guildSetting.GuildId != ulong.Parse(discordBotConfig.Value.AdminServerId)) continue;

                    foreach (News news in dataScrapingResult.UpdatedNews.Concat(dataScrapingResult.NewNews))
                    {
                        await discordApiHelper.SendFile(guildSetting.GuildId, guildSetting.DataScapingNewsChannelId, news.SnapshotTempFile.FullName, embed: EmbedUtil.GetMainogiNewsEmbed(news));
                    }
                }

                await command.FollowupAsync("Done");
            }
            catch (Exception ex)
            {
                await command.FollowupAsync(ex.ToString());
            }
        }
    }
}
