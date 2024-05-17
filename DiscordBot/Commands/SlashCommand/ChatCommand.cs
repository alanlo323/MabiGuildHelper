using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class ChatCommand(ILogger<ChatCommand> logger, DiscordSocketClient client, SemanticKernelEngine semanticKernelEngine, DatabaseHelper databaseHelper, ButtonHandlerHelper buttonHandlerHelper) : IBaseSlashCommand
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
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            await command.DeferAsync();

            DateTime startTime = DateTime.Now;
            string prompt = command.Data.Options.First(x => x.Name == "text").Value as string;
            RestFollowupMessage restFollowupMessage = null;
            object lockObj = new();

            KernelStatus kernelStatus = await semanticKernelEngine.GenerateResponse(prompt, OnKenelStatusUpdated);
            Conversation conversation= kernelStatus.Conversation;

            string responseMessage = GetResponseMessage(kernelStatus);
            var answer = responseMessage ?? string.Empty;
            answer = answer[..Math.Min(2000, answer.Length)];
            MessageComponent addReminderButtonComponent = buttonHandlerHelper.GetButtonHandler<PromptDetailButtonHandler>().GetMessageComponent();
            await FollowUpOrEditMessage(answer, components: addReminderButtonComponent);

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
                Dictionary<string, string> replacementDict = new() { { "memory-Ask", "在長期記憶尋找相關資料" } };
                List<string> statusList = [];
                foreach (var stepStatus in kernelStatus.StepStatuses)
                {
                    string displayName = stepStatus.Name;
                    foreach (var replacement in replacementDict) displayName = displayName.Replace(replacement.Key, replacement.Value);

                    string message = $"{stepStatus.Name} is {stepStatus.Status}";
                    switch (stepStatus.Status)
                    {
                        case StatusEnum.Pending:
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
                    statusList.Add(message);
                }

                string stepStatusMessage = string.Join(Environment.NewLine, statusList);
                string responseMessage = $"{stepStatusMessage}";
                if (!string.IsNullOrWhiteSpace(kernelStatus.Conversation.Result)) responseMessage += $"{Environment.NewLine}{Environment.NewLine}{kernelStatus.Conversation.Result}";

                return responseMessage;
            }
            #endregion
        }
    }
}