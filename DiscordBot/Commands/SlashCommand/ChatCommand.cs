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
using DiscordBot.KernelMemory;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class ChatCommand(ILogger<ChatCommand> logger, DiscordSocketClient client, KernelMemoryEngine kernelMemoryEngine) : IBaseSlashCommand
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
            try
            {
                string text = command.Data.Options.First(x => x.Name == "text").Value as string;
                var answer = await kernelMemoryEngine.AskAsync(text);
                string response = answer.Result;
                foreach (var x in answer.RelevantSources.OrderByDescending(x => x.Partitions.First().Relevance))
                {
                    var firstPartition = x.Partitions.First();
                    response += $"{Environment.NewLine}  * [{firstPartition.Relevance:P}] {(x.SourceUrl ?? x.SourceName)} -- {firstPartition.LastUpdate:D}";
                }
                await command.FollowupAsync(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ChatCommand Error");
                await command.FollowupAsync("語言模組發生錯誤, 請稍後再試");
            }
        }
    }
}
