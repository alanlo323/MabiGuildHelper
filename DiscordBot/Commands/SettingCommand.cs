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
        IOptionsSnapshot<GameConfig> _gameConfigSnapshot;
        IServiceProvider _serviceProvider;
        ImgurHelper _imgurHelper;
        DatabaseHelper _databaseHelper;

        public string Name { get; set; } = "setting";
        public string Description { get; set; } = "設定";

        public SettingCommand(ILogger<SettingCommand> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig, IServiceProvider serviceProvider, ImgurHelper imgurHelper, DatabaseHelper databaseHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _gameConfigSnapshot = gameConfig;
            _serviceProvider = serviceProvider;
            _imgurHelper = imgurHelper;
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
                        .WithName("instancereset")
                        .WithDescription("任務重置提醒")
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
                    case "instancereset":
                        await HandleInstanceResetCommand(command, subOption);
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

        private async Task HandleInstanceResetCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = await SetChannelId(command.GuildId.Value, option, nameof(GuildSetting.InstanceResetChannelId));
            await command.FollowupAsync($"已設定{optionChannel.Mention}為任務重置提醒頻道", ephemeral: true);
        }
    }
}
