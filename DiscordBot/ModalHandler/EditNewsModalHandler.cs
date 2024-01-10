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
        public static string CustomIdPrefix { get; set; } = "EditNewsModal";

        public string CustomId { get; set; } = CustomIdPrefix;

        public async Task Excute(SocketModal modal)
        {
            ulong messageId = ulong.Parse(modal.Data.CustomId.Split("_")[1]);
            IMessage message = await modal.Channel.GetMessageAsync(messageId);
            if (message is RestUserMessage userMessage)
            {
                await userMessage.ModifyAsync(x =>
                {
                    var title = modal.Data.Components.Where(x => x.CustomId.StartsWith(ModalUtil.EditNewsModalTitleIdPrefix)).Single();
                    var content = modal.Data.Components.Where(x => x.CustomId.StartsWith(ModalUtil.EditNewsModalContentIdPrefix)).Single();
                    var releatedMessageUrl = modal.Data.Components.Where(x => x.CustomId.StartsWith(ModalUtil.EditNewsModalReleatedMessageUrlPrefix)).Single();
                    x.Embed = embed;
                });
                return;
            }
        }
    }
}
