using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.DataObject;
using DiscordBot.SemanticKernel.Core;
using DiscordBot.SemanticKernel;
using DiscordBot.Util;
using Microsoft.Identity.Client;
using DiscordBot.Db.Entity;
using DiscordBot.Commands.SlashCommand;
using Humanizer;
using Microsoft.Extensions.Logging;
using DiscordBot.Extension;
using DiscordBot.Db;
using Newtonsoft.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using MongoDB.Bson;
using Microsoft.SemanticKernel;

namespace DiscordBot.Helper
{
    public class AiChatHelper(ILogger<AiChatHelper> logger, SemanticKernelEngine semanticKernelEngine, AppDbContext appDbContext, DatabaseHelper databaseHelper, ButtonHandlerHelper buttonHandlerHelper, EnchantmentHelper enchantmentHelper, ItemHelper itemHelper)
    {
        public async Task ProcessChatRequest(SocketInteraction socketInteraction, string prompt, Uri? imageUri = null, int? lastConversationId = null)
        {
            await socketInteraction.DeferAsync();

            if (imageUri != null && !await imageUri.IsImageUrl())
            {
                await socketInteraction.FollowupAsync("附件只支持圖片類型, 請檢查已選擇的附件");
                return;
            }

            DateTime startTime = DateTime.Now;

            RestFollowupMessage restFollowupMessage = null;

            #region Check Enchantment
            if (prompt.StartsWith("魔力賦予"))
            {
                string enchantmentName = enchantmentHelper.GetEnchantmentName(prompt);
                EnchantmentResponseDto enchantmentResponseDto = await enchantmentHelper.GetEnchantmentsAsync(enchantmentName);
                Enchantment enchantment = enchantmentResponseDto.Data.Enchantments.SingleOrDefault(x => x.LocalName == enchantmentName);
                if (enchantment != default)
                {
                    Embed enchantmentEmbed = EmbedUtil.GetEnchantmentEmbed(enchantment);
                    FollowUpOrEditMessage(socketInteraction, default!, ref restFollowupMessage, embed: enchantmentEmbed);
                    return;
                }
            }
            #endregion

            #region Check Item
            if (prompt.StartsWith("物品"))
            {
                string itemName = itemHelper.GetItemName(prompt);
                ItemSearchResponseDto itemResponseDto = await itemHelper.GetItemAsync(itemName, withScreenshot: true, withProductionInfo: true);
                Item item = itemResponseDto.Data.Items.SingleOrDefault(x => x.TextName1 == itemName);
                if (item != default)
                {
                    Embed itemEmbed = EmbedUtil.GetItemEmbed(item);
                    FollowUpOrEditMessage(socketInteraction, default!, ref restFollowupMessage, fileInfo: item.SnapshotTempFile, embed: itemEmbed);
                    return;
                }
            }
            #endregion

            Conversation lastConversation = appDbContext.Conversations.SingleOrDefault(x => x.Id == lastConversationId);
            ChatHistory? conversationChatHistory = null;
            if (lastConversation != null) conversationChatHistory = lastConversation.ChatHistoryJson.Deserialize<ChatHistory>();

            KernelStatus kernelStatus = await semanticKernelEngine.GenerateResponse(prompt, socketInteraction, imageUri: imageUri, conversationChatHistory: conversationChatHistory, onKenelStatusUpdatedCallback: OnKenelStatusUpdated);
            await Task.Delay(1000); // Wait for the status message to be sent

            string responseMessage = GetResponseMessage(kernelStatus);
            var answer = responseMessage ?? string.Empty;
            answer = answer[..Math.Min(2000, answer.Length)];
            MessageComponent conversationActionButtonComponent = buttonHandlerHelper.GetButtonHandler<ConversationActionButtonHandler>().GetMessageComponent();
            FollowUpOrEditMessage(socketInteraction, answer, ref restFollowupMessage, components: conversationActionButtonComponent);

            kernelStatus.Conversation.DiscordMessageId = restFollowupMessage!.Id;
            await databaseHelper.Add(kernelStatus.Conversation);
            await databaseHelper.SaveChange();

            #region Local Functions
            void OnKenelStatusUpdated(object? sender, KernelStatus kernelStatus)
            {
                string responseMessage = GetResponseMessage(kernelStatus);
                FollowUpOrEditMessage(socketInteraction, responseMessage, ref restFollowupMessage);
            }
            #endregion
        }

        private void FollowUpOrEditMessage(SocketInteraction socketInteraction, string content, ref RestFollowupMessage? restFollowupMessage, FileInfo? fileInfo = null, MessageComponent? components = null, Embed? embed = null)
        {
            if (restFollowupMessage == null)
            {
                restFollowupMessage = fileInfo == default
                    ? socketInteraction.FollowupAsync(content, components: components, embed: embed).GetAwaiter().GetResult()
                    : socketInteraction.FollowupWithFileAsync(filePath: fileInfo.FullName, fileName: fileInfo.Name, text: content, components: components, embed: embed).GetAwaiter().GetResult();
            }
            else
            {
                restFollowupMessage.ModifyAsync(x =>
                {
                    x.Content = content;
                    x.Components = components;
                    x.Embed = embed;
                    if (fileInfo != default) x.Attachments = new List<FileAttachment>() { new(path: fileInfo.FullName, fileName: fileInfo.Name) };
                }).GetAwaiter().GetResult();
            }
        }

        private string GetResponseMessage(KernelStatus kernelStatus)
        {
            Dictionary<string, string> replacementDict = new() {
                    { "memory-Ask", "搜尋核心記憶" },
                    { "Writer-Translate", "翻譯文本" },
                    { "MathPlugin", "數學計算" },
                    { "WebPlugin-GoogleSearch", "搜尋網路資料" },
                    { "WebPlugin-BingAiSearch", "進階網路搜尋" },
                    { "WebPlugin-GetWebContent", "獲取網頁內容" },
                    { "ConversationSummaryPlugin-FindRelatedInformationWithGoal", "分析資料" },
                    { "FindRelatedInformationWithGoal", "尋找相關內容" },
                    { "AboutPlugin-GetBackgroundInformation", "獲得背景資料" },
                    { "ConversationSummaryPlugin-SummarizeConversation", "總結內容" },
                    { "CreatePlan", "製定計劃" },
                    { nameof(StatusEnum.Thinking), "思考中" },
                    { nameof(StatusEnum.Pending), "等待處理" },
                    { "TimePlugin-Now", "獲取當前時間" },
                    { "TimePlugin-Today", "獲取當前日期" },
                    { "TimePlugin-TimeZoneName", "獲取當前時區" },
                    { "CodeInterpretionPlugin-ExecutePythonCode", "執行Python程式碼" },
                    { "EnchantmentPlugin-GetEnchantmentInfo", "查詢魔力賦予API" },
                    { "Internal Error", "發生內部錯誤" },
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
                    case StatusEnum.Pending:
                        message = $"🕘 {displayName}";
                        break;
                    case StatusEnum.Thinking:
                        message = $"💭 {displayName}";
                        break;
                    case StatusEnum.Running:
                        message = $"⌛ ✨{displayName}✨";
                        break;
                    case StatusEnum.Completed:
                        message = $"✅ ✨{displayName}✨";
                        break;
                    case StatusEnum.Failed:
                        message = $"❌ ✨{displayName}✨";
                        break;
                    case StatusEnum.Error:
                        message = $"⚠️ {displayName}";
                        break;
                    default:
                        break;
                }
                message += $"{stepStatus.KernelArguments.ToDisplayName()}";
                if (stepStatus.ElapsedTime.HasValue) message += $" ({stepStatus.ElapsedTime?.Humanize(precision: 2, minUnit: Humanizer.Localisation.TimeUnit.Second, collectionSeparator: " ", culture: new CultureInfo("zh-tw"))})";
                statusList.Add(message);
            }

            string stepStatusMessage = string.Join(Environment.NewLine, statusList);
            StringBuilder responseBuilder = new();
            responseBuilder.AppendLine(kernelStatus.Conversation?.UserPrompt?.TrimToLimited(200).ToQuotation());
            responseBuilder.AppendJoin(Environment.NewLine, statusList);
            if (statusList.Count > 0) responseBuilder.Append($"{Environment.NewLine}{Environment.NewLine}");
            if (!string.IsNullOrWhiteSpace(kernelStatus.Conversation?.Result)) responseBuilder.Append(kernelStatus.Conversation?.Result);

            string responseMessage = responseBuilder.ToString();
            return responseMessage;
        }
    }
}
