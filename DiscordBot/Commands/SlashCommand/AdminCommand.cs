using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class AdminCommand(ILogger<AdminCommand> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper) : IBaseSlashCommand
    {
        public string Name { get; set; } = "admin";
        public string Description { get; set; } = "管理員功能";
        public CommandAvailability Availability { get; set; } = CommandAvailability.AdminServerOnly;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("setting")
                    .WithDescription("設定")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("attachment")
                        .WithDescription("附件")
                        .WithType(ApplicationCommandOptionType.SubCommandGroup)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: [ChannelType.Text])
                    )
                )
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            foreach (SocketSlashCommandDataOption option in command.Data.Options)
            {
                switch (option.Name)
                {
                    case "setting":
                        await HandleSettingCommand(command, option);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleSettingCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            foreach (SocketSlashCommandDataOption subOption in option.Options)
            {
                switch (subOption.Name)
                {
                    case "attachment":
                        await HandleAttachmentCommand(command, subOption);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task<SocketTextChannel> SetChannelId(ulong guildId, SocketSlashCommandDataOption option, string channelIdPropertyName)
        {
            SocketTextChannel optionChannel = option.Options.First(x => x.Name == "channel").Value as SocketTextChannel;
            SocketGuild socketGuild = client.GetGuild(guildId);

            var guildSetting = await databaseHelper.GetOrCreateEntityByKeys<GuildSetting>(new() { { nameof(GuildSetting.GuildId), socketGuild.Id } });
            guildSetting.SetProperty(channelIdPropertyName, optionChannel.Id);
            appDbContext.SaveChanges();

            return optionChannel;
        }

        private async Task HandleAttachmentCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = await SetChannelId(command.GuildId.Value, option, nameof(GlobalSetting.AttachmentChannelId));
            await command.RespondAsync($"已設定{optionChannel.Mention}為附件頻道", ephemeral: true);
        }
    }
}
