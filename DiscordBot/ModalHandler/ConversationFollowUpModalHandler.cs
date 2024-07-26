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

namespace DiscordBot.ModalHandler
{
    public class ConversationFollowUpModalHandler(ILogger<ConversationFollowUpModalHandler> logger, AiChatHelper aiChatHelper) : IBaseModalHandler
    {
        public static readonly string ConversationFollowUpModalMasterIdPrefix = "ConversationFollowUpModal";
        public static readonly string ConversationFollowUpModalContentIdPrefix = $"{ConversationFollowUpModalMasterIdPrefix}_Content";

        public string CustomId { get; set; } = ConversationFollowUpModalMasterIdPrefix;

        public async Task Execute(SocketModal modal)
        {
            try
            {
                var prompt = modal.Data.Components.Where(x => x.CustomId == ConversationFollowUpModalContentIdPrefix).Single().Value;
                int conversationId = int.Parse(modal.Data.CustomId.Split("_")[1]);

                await aiChatHelper.ProcessChatRequest(modal, prompt, lastConversationId: conversationId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await modal.FollowupAsync("小幫手發生未知錯誤, 請通知作者!");
            }
        }
    }
}
