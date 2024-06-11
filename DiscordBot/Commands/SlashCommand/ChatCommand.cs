using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel;
using DiscordBot.SemanticKernel.Core;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DiscordBot.Util;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class ChatCommand(ILogger<ChatCommand> logger, DiscordSocketClient client, SemanticKernelEngine semanticKernelEngine, DatabaseHelper databaseHelper, ButtonHandlerHelper buttonHandlerHelper, IOptionsSnapshot<DiscordBotConfig> discordBotConfig) : IBaseSlashCommand
    {
        public string Name { get; set; } = "chat";
        public string Description { get; set; } = "和小幫手對話";
        public CommandAvailability Availability { get; set; } = CommandAvailability.Global;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .AddOption("text", ApplicationCommandOptionType.String, "內容", isRequired: true, minLength: 1)
                .AddOption("attachment", ApplicationCommandOptionType.Attachment, "附件 (只限圖片)", isRequired: false)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            await command.DeferAsync();

            DateTime startTime = DateTime.Now;
            string prompt = command.Data.Options.First(x => x.Name == "text").Value as string;
            Uri imageUri = command.Data.Options.FirstOrDefault(x => x.Name == "attachment")?.Value is Attachment attachment ? new Uri(attachment.ProxyUrl) : null;
            if (await imageUri.IsImageUrl() != true)
            {
                await command.FollowupAsync("附件只支持圖片類型, 請檢查已選擇的附件");
                return;
            }

            bool showStatusPerSec = true || command.User.Id == ulong.Parse(discordBotConfig.Value.AdminId);
            RestFollowupMessage restFollowupMessage = null;
            object lockObj = new();

            KernelStatus kernelStatus = await semanticKernelEngine.GenerateResponse(prompt, command, imageUri: imageUri, showStatusPerSec: showStatusPerSec, onKenelStatusUpdatedCallback: OnKenelStatusUpdated);
            Conversation conversation = kernelStatus.Conversation;

            string responseMessage = GetResponseMessage(kernelStatus);
            var answer = responseMessage ?? string.Empty;
            answer = answer[..Math.Min(2000, answer.Length)];
            MessageComponent addReminderButtonComponent = buttonHandlerHelper.GetButtonHandler<PromptDetailButtonHandler>().GetMessageComponent();
            await FollowUpOrEditMessage(answer, components: addReminderButtonComponent);

            //  TODO: Add support of reply message to continue the conversation

            conversation.DiscordMessageId = restFollowupMessage.Id;
            await databaseHelper.Add(conversation);
            await databaseHelper.SaveChange();

            #region Local Functions
            async void OnKenelStatusUpdated(object? sender, KernelStatus kernelStatus)
            {
                string responseMessage = GetResponseMessage(kernelStatus);
                await FollowUpOrEditMessage(responseMessage);
            }

            async Task FollowUpOrEditMessage(string message, MessageComponent? components = null)
            {
                lock (lockObj)
                {
                    if (restFollowupMessage == null)
                    {
                        restFollowupMessage = command.FollowupAsync(message, components: components).GetAwaiter().GetResult();
                    }
                    else
                    {
                        restFollowupMessage.ModifyAsync(x =>
                        {
                            x.Content = message;
                            x.Components = components;
                        }).GetAwaiter().GetResult();
                    }
                }
            }

            string GetResponseMessage(KernelStatus kernelStatus)
            {
                Dictionary<string, string> replacementDict = new() {
                    { "memory-Ask", "搜尋核心記憶" },
                    { "Writer-Translate", "翻譯文本" },
                    { "MathPlugin", "數學計算" },
                    { "WebSearchPlugin-Search", "搜尋網路資料" },
                    { "WebSearchPlugin-BingSearch", "搜尋網路資料" },
                    { "ConversationSummaryPlugin-FindRelatedInformationWithGoal", "分析資料" },
                    { "FindRelatedInformationWithGoal", "尋找相關內容" },
                    { "AboutPlugin-GetBackgroundInformation", "獲得背景資料" },
                    { "ConversationSummaryPlugin-SummarizeConversation", "總結內容" },
                    { "CreatePlan", "製定計劃" },
                    { "Thinking", "思考中" },
                    { "TimePlugin-Now", "獲取當前時間" },
                    { "TimePlugin-TimeZoneName", "獲取當前時區" },
                };
                List<string> ignoreList = [
                    "GetBackgroundInformation",
                    "FindRelatedInformationWithGoal",
                    "SummarizeConversation",
                ];
                List<string> statusList = [];
                foreach (var stepStatus in kernelStatus.StepStatuses)
                {
                    if (ignoreList.Any(x => stepStatus.Key == x)) continue;

                    string displayName = stepStatus.DisplayName;
                    foreach (var replacement in replacementDict) displayName = displayName.Replace(replacement.Key, replacement.Value);

                    string message = $"{displayName} is {stepStatus.Status}";
                    switch (stepStatus.Status)
                    {
                        case StatusEnum.Thinking:
                            message = $"💭{displayName}";
                            break;
                        case StatusEnum.Running:
                            message = $"⌛{displayName}";
                            break;
                        case StatusEnum.Completed:
                            message = $"✅ {displayName}";
                            break;
                        case StatusEnum.Failed:
                            break;
                        default:
                            break;
                    }
                    if (stepStatus.ElapsedTime.HasValue) message += $" ({stepStatus.ElapsedTime?.Humanize(precision: 2, minUnit: Humanizer.Localisation.TimeUnit.Second, collectionSeparator: " ", culture: new CultureInfo("zh-tw"))})";
                    statusList.Add(message);
                }

                string stepStatusMessage = string.Join(Environment.NewLine, statusList);
                string responseMessage = $"{stepStatusMessage}";
                if (!string.IsNullOrWhiteSpace(kernelStatus.Conversation?.Result)) responseMessage += $"{Environment.NewLine}{Environment.NewLine}{kernelStatus.Conversation?.Result}";

                return responseMessage;
            }
            #endregion
        }
    }
}