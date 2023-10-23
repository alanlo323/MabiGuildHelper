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
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
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
        GameConfig _gameConfig;
        IOptionsSnapshot<GameConfig> _gameConfigSnapshot;
        IServiceProvider _serviceProvider;

        public string Name { get; set; } = "setting";
        public string Description { get; set; } = "設定";

        public SettingCommand(ILogger<SettingCommand> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _gameConfig = gameConfig.Value;
            _gameConfigSnapshot = gameConfig;
            _serviceProvider = serviceProvider;
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
                //.AddOption(new SlashCommandOptionBuilder()
                //    .WithName("eventinfo")
                //    .WithDescription("活動資訊")
                //    .WithType(ApplicationCommandOptionType.SubCommand)
                //    .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
                //)
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
                        await HandleInformationCommand(command, option);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleInformationCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
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
                    default:
                        break;
                }
            }
        }

        private async Task HandleErinnTimeCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = option.Options.First(x => x.Name == "channel").Value as SocketTextChannel;
            SocketGuild guild = _client.GetGuild(command.GuildId.Value);

            var guildInDb = _appDbContext.GuildSettings
                .Where(x => x.GuildId == guild.Id)
                .FirstOrDefault();
            if (guildInDb == null)
            {
                GuildSetting newGuild = new()
                {
                    GuildId = guild.Id,
                    ErinnTimeChannelId = optionChannel.Id,
                };
                await _appDbContext.AddAsync(newGuild);
                _logger.LogInformation($"Added GuildSetting in db:");
                _logger.LogInformation($"{newGuild.ToJsonString()}");

                guildInDb = newGuild;
            }
            else
            {
                guildInDb.ErinnTimeChannelId = optionChannel.Id;
                _appDbContext.Update(guildInDb);
                _logger.LogInformation($"Update GuildSetting in db:");
                _logger.LogInformation($"{guildInDb.ToJsonString()}");
            }

            _appDbContext.SaveChanges();

            await command.RespondAsync($"已設定{optionChannel.Mention}為愛爾琳時間頻道", ephemeral: true);

            ErinnTimeJob job = new(_serviceProvider.GetRequiredService<ILogger<ErinnTimeJob>>(), _client, _appDbContext);
            await job.Execute(null);
        }

        private async Task HandleDailyEffectCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketTextChannel optionChannel = option.Options.First(x => x.Name == "channel").Value as SocketTextChannel;
            SocketGuild guild = _client.GetGuild(command.GuildId.Value);

            var guildInDb = _appDbContext.GuildSettings
                .Where(x => x.GuildId == guild.Id)
                .FirstOrDefault();
            if (guildInDb == null)
            {
                GuildSetting newGuild = new()
                {
                    GuildId = guild.Id,
                    DailyEffectChannelId = optionChannel.Id,
                };
                await _appDbContext.AddAsync(newGuild);
                _logger.LogInformation($"Added GuildSetting in db:");
                _logger.LogInformation($"{newGuild.ToJsonString()}");

                guildInDb = newGuild;
            }
            else
            {
                guildInDb.DailyEffectChannelId = optionChannel.Id;
                _appDbContext.Update(guildInDb);
                _logger.LogInformation($"Update GuildSetting in db:");
                _logger.LogInformation($"{guildInDb.ToJsonString()}");
            }

            _appDbContext.SaveChanges();

            await command.RespondAsync($"已設定{optionChannel.Mention}為日期效果頻道", ephemeral: true);

            DailyEffectJob job = new(_serviceProvider.GetRequiredService<ILogger<DailyEffectJob>>(), _client, _appDbContext, _gameConfigSnapshot);
            await job.Execute(null);
        }
    }
}
