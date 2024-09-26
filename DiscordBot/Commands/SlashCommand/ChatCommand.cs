using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.Configuration;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel;
using DiscordBot.SemanticKernel.Core;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DiscordBot.SemanticKernel.QueneService;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class ChatCommand(ILogger<ChatCommand> logger, DiscordSocketClient client, AiChatHelper aiChatHelper) : IBaseSlashCommand
    {
        public string Name { get; set; } = "chat";
        public string Description { get; set; } = "和小幫手對話";
        public CommandAvailability Availability { get; set; } = CommandAvailability.Global;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .AddOption("text", ApplicationCommandOptionType.String, "內容", isRequired: true, minLength: 1, isAutocomplete: true)
                .AddOption("attachment", ApplicationCommandOptionType.Attachment, "附件 (只限圖片)", isRequired: false)
                ;
            return command.Build();
        }

        public async Task Execute(SocketSlashCommand command)
        {
            string prompt = command.Data.Options.First(x => x.Name == "text").Value as string;
            Uri imageUri = command.Data.Options.FirstOrDefault(x => x.Name == "attachment")?.Value is Attachment attachment ? new Uri(attachment.ProxyUrl) : null;

            await aiChatHelper.ProcessChatRequest(command, prompt, imageUri);
        }
    }
}