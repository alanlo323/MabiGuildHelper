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

            var result = await semanticKernelEngine.GenerateResponse(prompt);
            var answer = result.Item1;
            var planTemplate = result.Item2;

            answer = answer[..Math.Min(2000, answer.Length)];
            MessageComponent addReminderButtonComponent = buttonHandlerHelper.GetButtonHandler<PromptDetailButtonHandler>().GetMessageComponent();
            RestFollowupMessage restFollowupMessage = await command.FollowupAsync(answer, components: addReminderButtonComponent);
            //RestFollowupMessage restFollowupMessage = await command.FollowupAsync(answer);

            var conversation = await databaseHelper.GetOrCreateEntityByKeys<Conversation>(new() { { nameof(Conversation.DiscordMessageId), restFollowupMessage.Id } });
            conversation.UserPrompt = prompt;
            conversation.Result = answer;
            conversation.PlanTemplate = planTemplate;
            conversation.StartTime = startTime;
            conversation.EndTime = DateTime.Now;
            await databaseHelper.SaveChange();
        }
    }
}
