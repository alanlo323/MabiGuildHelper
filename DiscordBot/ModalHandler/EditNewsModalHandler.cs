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
using DiscordBot.SelectMenuHandler;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.ButtonHandler
{
    public class EditNewsModalHandler(ILogger<EditNewsModalHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, SelectMenuHandlerHelper selectMenuHandlerHelper) : IBaseModalHandler
    {
        public static readonly string EditNewsModalMasterIdPrefix = "EditNewsModal";
        public static readonly string EditNewsModalTitleIdPrefix = $"{EditNewsModalMasterIdPrefix}_Title";
        public static readonly string EditNewsModaItemTagPrefix = $"{EditNewsModalMasterIdPrefix}_ItemTag";
        public static readonly string EditNewsModalContentIdPrefix = $"{EditNewsModalMasterIdPrefix}_Content";
        public static readonly string EditNewsModalReleatedMessageUrlPrefix = $"{EditNewsModalMasterIdPrefix}_ReleatedMessageUrl";

        public string CustomId { get; set; } = EditNewsModalMasterIdPrefix;

        public async Task Excute(SocketModal modal)
        {
            try
            {
                ulong messageId = ulong.Parse(modal.Data.CustomId.Split("_")[1]);
                string newsUrl = modal.Data.CustomId.Split("_")[2];
                IMessage message = await modal.Channel.GetMessageAsync(messageId);
                if (message is RestUserMessage userMessage)
                {
                    var title = modal.Data.Components.Where(x => x.CustomId == EditNewsModalTitleIdPrefix).Single().Value;
                    var content = modal.Data.Components.Where(x => x.CustomId == EditNewsModalContentIdPrefix).Single().Value;
                    var releatedMessageUrl = modal.Data.Components.Where(x => x.CustomId == EditNewsModalReleatedMessageUrlPrefix).Single().Value;
                    var embedBuilder = userMessage.Embeds.Single().ToEmbedBuilder();

                    var guildNewsOverride = await databaseHelper.GetOrCreateEntityByKeys<GuildNewsOverride>(new() { { nameof(GuildNewsOverride.GuildId), modal.GuildId }, { nameof(GuildNewsOverride.Url), newsUrl } });
                    guildNewsOverride.Title = title;
                    guildNewsOverride.Content = content;
                    guildNewsOverride.ReleatedMessageUrl = releatedMessageUrl;
                    await appDbContext.SaveChangesAsync();

                    embedBuilder = embedBuilder
                        .WithTitle(title)
                        .WithDescription(string.IsNullOrWhiteSpace(releatedMessageUrl) ? content : $"{content}{Environment.NewLine}{Environment.NewLine}維護資訊:{releatedMessageUrl}")
                        .WithCurrentTimestamp();

                    await userMessage.ModifyAsync(x =>
                    {
                        x.Embed = embedBuilder.Build();
                        x.Attachments = new List<FileAttachment>();
                    });
                    await modal.RespondAsync("通告編輯成功", ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await modal.RespondAsync("小幫手發生未知錯誤, 請通知作者!", ephemeral: true);
            }
        }
    }
}
