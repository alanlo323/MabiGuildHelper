using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
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
    public class ChatCommand(ILogger<ChatCommand> logger, DiscordSocketClient client, SemanticKernelEngine semanticKernelEngine) : IBaseSlashCommand
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

            string prompt = command.Data.Options.First(x => x.Name == "text").Value as string;
            var answer = await semanticKernelEngine.GenerateResponse(prompt);
            answer = answer[..Math.Min(2000, answer.Length)];
            await command.FollowupAsync(answer);
        }
    }
}
