using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel.Core;
using DiscordBot.SemanticKernel;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using static DiscordBot.Commands.IBaseCommand;
using Discord.Rest;
using Quartz.Util;
using Microsoft.SemanticKernel;
using System.Threading;

namespace DiscordBot.Commands.MessageCommand
{
    public class SummarizeCommand(ILogger<SummarizeCommand> logger, DiscordSocketClient client, AppDbContext appDbContext, DatabaseHelper databaseHelper, SemanticKernelEngine semanticKernelEngine) : IBaseMessageCommand
    {
        public string Name { get; set; } = "總結內容";
        public string Description { get; set; }
        public CommandAvailability Availability { get; set; } = CommandAvailability.AdminServerOnly;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new MessageCommandBuilder()
                .WithName(Name)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                ;
            return command.Build();
        }

        public async Task Excute(SocketMessageCommand command)
        {
            try
            {
                await command.DeferAsync();

                var message = command.Data.Message;
                Embed embed = message.Embeds.FirstOrDefault();
                var input = (string.IsNullOrWhiteSpace(embed?.Description) ? message.Content : embed.Description);

                var kernel = await semanticKernelEngine.GetKernelAsync();
                string result = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "SummarizeMabiNews", arguments: new()
                {
                    { "input", input },
                    { "kernel", kernel },
                });

                await command.FollowupAsync(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }
}
