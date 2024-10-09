using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using DiscordBot.SemanticKernel;
using DiscordBot.Util;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using static DiscordBot.Commands.IBaseCommand;
using static DiscordBot.SemanticKernel.SemanticKernelEngine;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.Identity.Client;
using Microsoft.KernelMemory.Pipeline;

namespace DiscordBot.Commands.SlashCommand
{
    public class DebugCommand(ILogger<DebugCommand> logger, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, AppDbContext appDbContext, DiscordApiHelper discordApiHelper, DataScrapingHelper dataScrapingHelper, SemanticKernelEngine semanticKernelEngine) : IBaseSlashCommand
    {
        public string Name { get; set; } = "debug";
        public string Description { get; set; } = "測試";
        public CommandAvailability Availability { get; set; } = CommandAvailability.AdminServerOnly;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                ;
            return command.Build();
        }

        public async Task Execute(SocketSlashCommand command)
        {
            ulong[] allowedUser = [ulong.Parse(discordBotConfig.Value.AdminId)];
            if (!allowedUser.Contains(command.User.Id))
            {
                await command.RespondAsync("你沒有權限使用此指令", ephemeral: true);
                return;
            }

            await command.DeferAsync();
            try
            {
                News news = appDbContext.News.First(x => x.Id == 7);

                var result = await semanticKernelEngine.GenerateResponse(Usage.DataScrapingJob, news.HtmlContent);

                await command.FollowupAsync(result.Conversation.Result);
            }
            catch (Exception ex)
            {
                await command.FollowupAsync(ex.ToString());
            }
        }
    }
}
