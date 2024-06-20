using System;
using System.Collections.Generic;
using System.Globalization;
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
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DiscordBot.Helper.PromptHelper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.ButtonHandler
{
    public class PromptDetailButtonHandler(ILogger<PromptDetailButtonHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, SelectMenuHandlerHelper selectMenuHandlerHelper, PromptHelper promptHelper) : IBaseButtonHandler
    {
        public const string PromptDetailButtonIdLabel = "詳細資訊";
        public const string PromptDetailButtonId = "PromptDetailButton";

        public string[] Lables { get; set; } = [PromptDetailButtonIdLabel];
        public string[] Ids { get; set; } = [PromptDetailButtonId];

        public MessageComponent GetMessageComponent()
        {
            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton(label: PromptDetailButtonIdLabel, emote: new Emoji("💭"), style: ButtonStyle.Primary, customId: PromptDetailButtonId);
            return componentBuilder.Build();
        }

        public async Task Excute(SocketMessageComponent component)
        {
            await component.DeferAsync(ephemeral: true);
            var conversation = await databaseHelper.GetOrCreateEntityByKeys<Conversation>(new() { { nameof(Conversation.DiscordMessageId), component.Message.Id } });

            StringBuilder sb = new();
            StringBuilder innerSb = new();
            StringBuilder promptSb = new();
            var planSteps = promptHelper.GetPlanStepFromString(conversation.PlanTemplate);
            if (planSteps.Count == 0) innerSb.AppendLine(conversation.PlanTemplate);
            foreach (var planStep in planSteps)
            {
                innerSb.AppendLine($"{planStep.FullDisplayName}");
                foreach (var rows in planStep.DisplayActionRows) innerSb.AppendLine($"{rows}");
            }
            promptSb.AppendLine($"{conversation.UserPrompt}".ToHighLight());
            sb.AppendLine(promptSb.ToString());
            sb.AppendLine(innerSb.ToString()[..Math.Min(1800 - promptSb.Length, innerSb.ToString().Length)].ToQuotation());
            sb.AppendLine($"**開始時間:** {conversation.StartTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**結束時間:** {conversation.EndTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**執行時間:** {conversation.ElapsedTime?.Humanize(precision: 2, minUnit: Humanizer.Localisation.TimeUnit.Second, collectionSeparator: " ", culture: new CultureInfo("zh-tw"))}");
            sb.AppendLine($"**Prompt Tokens:** {conversation.PromptTokens}");
            sb.AppendLine($"**Completion Tokens:** {conversation.CompletionTokens}");
            sb.AppendLine($"**Total Tokens:** {conversation.TotalTokens}");
            sb.AppendLine($"**估計成本:** {conversation.DisplayEstimatedCost}");
            await component.FollowupAsync(text: sb.ToString(), ephemeral: true);
        }
    }
}
