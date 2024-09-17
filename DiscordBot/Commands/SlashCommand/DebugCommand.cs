using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using DiscordBot.Util;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class DebugCommand(ILogger<DebugCommand> logger, AppDbContext appDbContext, DiscordApiHelper discordApiHelper, IOptionsSnapshot<DiscordBotConfig> discordBotConfig) : IBaseSlashCommand
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

        public async Task Excute(SocketSlashCommand command)
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
                IIntegrationChannel? channel = command.Channel as IIntegrationChannel; ;
                IGuildUser user = await channel!.GetUserAsync(command.User.Id);
                string avatarUrl = user.GetDisplayAvatarUrl();
                Stream avatarStream = await MiscUtil.GetStreamFromUrl(avatarUrl);
                var webhook = await channel.CreateWebhookAsync(user.DisplayName, avatar: avatarStream, options: new()
                {
                    AuditLogReason = $"Create user: {user.Nickname}[{user.Id}] Webhook",
                    RetryMode = RetryMode.AlwaysRetry,
                });
                DiscordWebhookClient client = new(webhook);
                await client.SendMessageAsync("Test");
                await client.DeleteWebhookAsync();

                await command.FollowupAsync("Done", ephemeral: true);
            }
            catch (Exception ex)
            {
                await command.FollowupAsync(ex.ToString());
            }
        }
    }
}
