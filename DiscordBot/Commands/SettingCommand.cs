using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class SettingCommand : IBaseCommand
    {
        ILogger<SettingCommand> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        IServiceProvider _serviceProvider;
        DatabaseHelper _databaseHelper;

        public string Name { get; set; } = "setting";
        public string Description { get; set; } = "設定";

        public SettingCommand(ILogger<SettingCommand> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _serviceProvider = serviceProvider;
            _databaseHelper = databaseHelper;
        }

        public SlashCommandProperties GetSlashCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("autoupdate")
                    .WithDescription("設定自動更新頻道")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("erinntime")
                        .WithDescription("愛爾琳時間")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("dailyeffect")
                        .WithDescription("今日資訊")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
                    )
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("dailydungeoninfo")
                        .WithDescription("老手地城")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
                    )
                )
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("reminder")
                    .WithDescription("設定提醒功能頻道")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("instanceresetreminder")
                        .WithDescription("重置提醒")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
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
                    case "autoupdate":
                        await HandleAutoUpdateCommand(command, option);
                        break;
                    case "reminder":
                        await HandleReminderCommand(command, option);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleAutoUpdateCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            foreach (SocketSlashCommandDataOption subOption in option.Options)
            {
                switch (subOption.Name)
                {
                    case "erinntime":
                        await HandleErinnTimeCommand(command, subOption);
                        break;
                    case "dailyeffect":
                        await HandleDailyEffectCommand(command, subOption);
                        break;
                    case "dailydungeoninfo":
                        await HandleDailyDungeonInfoCommand(command, subOption);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleReminderCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            foreach (SocketSlashCommandDataOption subOption in option.Options)
            {
                switch (subOption.Name)
                {
                    case "instanceresetreminder":
                        await HandleInstanceResetReminderCommand(command, subOption);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task<SocketTextChannel> SetChannelId(ulong guildId, SocketSlashCommandDataOption option, string channelIdPropertyName)
        {
            SocketTextChannel optionChannel = option.Options.First(x => x.Name == "channel").Value as SocketTextChannel;
            SocketGuild socketGuild = _client.GetGuild(guildId);

            var guildSetting = await _databaseHelper.GetOrCreateEntityByKeys<GuildSetting>(new() { { nameof(GuildSetting.GuildId), socketGuild.Id } });
            guildSetting.SetProperty(channelIdPropertyName, optionChannel.Id);
            _appDbContext.SaveChanges();

            return optionChannel;
        }

        private async Task HandleErinnTimeCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = await SetChannelId(command.GuildId.Value, option, nameof(GuildSetting.ErinnTimeChannelId));
            await command.RespondAsync($"已設定{optionChannel.Mention}為愛爾琳時間頻道", ephemeral: true);

            ErinnTimeJob job = _serviceProvider.GetRequiredService<ErinnTimeJob>();
            await job.Execute(null);
        }

        private async Task HandleDailyEffectCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = await SetChannelId(command.GuildId.Value, option, nameof(GuildSetting.DailyEffectChannelId));
            await command.RespondAsync($"已設定{optionChannel.Mention}為今日資訊頻道", ephemeral: true);

            DailyEffectJob job = _serviceProvider.GetRequiredService<DailyEffectJob>();
            await job.Execute(null);
        }

        private async Task HandleDailyDungeonInfoCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = await SetChannelId(command.GuildId.Value, option, nameof(GuildSetting.DailyDungeonInfoChannelId));
            await command.RespondAsync($"已設定{optionChannel.Mention}為老手地城頻道", ephemeral: true);

            DailyDungeonInfoJob job = _serviceProvider.GetRequiredService<DailyDungeonInfoJob>();
            await job.Execute(null);
        }

        private async Task HandleInstanceResetReminderCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = await SetChannelId(command.GuildId.Value, option, nameof(GuildSetting.InstanceResetReminderChannelId));
            await command.RespondAsync($"已設定{optionChannel.Mention}為重置提醒頻道", ephemeral: true);

            InstanceResetReminderJob job = _serviceProvider.GetRequiredService<InstanceResetReminderJob>();
            await job.Execute(null);
        }
    }
}
