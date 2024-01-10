using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Commands.MessageCommand
{
    public class EditNewsCommand(ILogger<EditNewsCommand> logger, DiscordSocketClient client, AppDbContext appDbContext, DatabaseHelper databaseHelper) : IBaseMessageCommand
    {
        public string Name { get; set; } = "編輯官方通告";
        public string Description { get; set; }

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new MessageCommandBuilder()
                .WithName(Name)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                ;
            return command.Build();
        }

        public async Task Excute(SocketMessageCommand command)
        {
            try
            {
                var message = command.Data.Message;
                if (message.Author.Id != client.CurrentUser.Id || message.Embeds.Count != 1)
                {
                    await command.RespondAsync("此指令只能對小幫手轉載的官方通告使用", ephemeral: true);
                    return;
                }

                Embed embed = message.Embeds.Single();
                var asd = appDbContext.News.Where(x => x.Url == embed.Url.Replace($"{DataScrapingHelper.MabinogiBaseUrl}/", string.Empty)).ToList();
                var bbb = appDbContext.News.ToList();
                News? news = appDbContext.News.Where(x => x.Url == embed.Url.Replace($"{DataScrapingHelper.MabinogiBaseUrl}/", string.Empty)).SingleOrDefault() ?? new()
                {
                    Url = embed.Url,
                    Title = embed.Title,
                    Content = embed.Description,
                };
                GuildNewsOverride newsOverride = appDbContext.GuildNewsOverrides.Where(x => x.GuildId == command.GuildId && x.Url == news.Url).SingleOrDefault() ?? GuildNewsOverride.CloneFromNews(news);
                if (newsOverride.GuildId == default)
                {
                    newsOverride.GuildId = (ulong)command.GuildId;
                    await appDbContext.AddAsync(newsOverride);
                    await appDbContext.SaveChangesAsync();
                }
                Modal modal = ModalUtil.GetEditNewsModal(newsOverride, message, Name);
                await command.RespondWithModalAsync(modal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }
}
