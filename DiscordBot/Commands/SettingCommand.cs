using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class SettingCommand : IBaseCommand
    {
        ILogger<SettingCommand> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;

        public string Name { get; set; } = "setting";
        public string Description { get; set; } = "設定";

        public SettingCommand(ILogger<SettingCommand> logger, DiscordSocketClient client, AppDbContext appDbContext)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
        }

        public SlashCommandProperties GetSlashCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("information")
                    .WithDescription("設定資訊頻道")
                    .WithType(ApplicationCommandOptionType.SubCommandGroup)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("erinntime")
                        .WithDescription("愛爾琳時間")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Voice })
                    ).AddOption(new SlashCommandOptionBuilder()
                        .WithName("todayinfo")
                        .WithDescription("今日資訊")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
                    ).AddOption(new SlashCommandOptionBuilder()
                        .WithName("eventinfo")
                        .WithDescription("活動資訊")
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
                    case "information":
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
                        await HandleErinntimeCommand(command, subOption);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task HandleErinntimeCommand(SocketSlashCommand command, SocketSlashCommandDataOption option)
        {
            SocketVoiceChannel optionChannel = option.Options.First(x => x.Name == "channel").Value as SocketVoiceChannel;
            SocketGuild guild = _client.GetGuild(command.GuildId.Value);

            var guildInDb = _appDbContext.GuildSettings
                .Where(x => x.GuildId == guild.Id)
                .FirstOrDefault();
            if (guildInDb == null)
            {
                GuildSetting newGuild = new()
                {
                    GuildId = guild.Id,
                    ErinntimeChannelId = optionChannel.Id,
                };
                await _appDbContext.AddAsync(newGuild);
                _logger.LogInformation($"Added GuildSetting in db:");
                _logger.LogInformation($"{newGuild.ToJsonString()}");
            }
            else
            {
                guildInDb.ErinntimeChannelId = optionChannel.Id;
                _appDbContext.Update(guildInDb);
                _logger.LogInformation($"Update GuildSetting in db:");
                _logger.LogInformation($"{guildInDb.ToJsonString()}");
            }

            Overwrite? everyonePermission = optionChannel.PermissionOverwrites.Where(x => x.TargetId == guild.EveryoneRole.Id).FirstOrDefault();
            if (!everyonePermission.HasValue) everyonePermission = new Overwrite(guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions());
            if (everyonePermission.Value.Permissions.Connect != PermValue.Deny)
            {
                await optionChannel.AddPermissionOverwriteAsync(guild.EveryoneRole, everyonePermission.Value.Permissions.Modify(connect: PermValue.Deny));
                _logger.LogInformation($"Updated EveryoneRole PermissionOverwrite for {optionChannel.Name}({optionChannel.Id}) in {guild.Name}{guild.Id}");
            }

            await _appDbContext.SaveChangesAsync();

            string oldName = optionChannel.Name;
            await optionChannel.ModifyAsync(x => x.Name = $"愛爾琳時間⏱ {GameUtil.GetErinnTime(roundToTenMins: true).ToString(@"hh\:mm")}");

            await command.RespondAsync($"已設定{optionChannel.Mention}為愛爾琳時間頻道", ephemeral: true);
        }
    }
}
