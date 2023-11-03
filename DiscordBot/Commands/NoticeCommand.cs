﻿using System;
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
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class NoticeCommand : IBaseCommand
    {
        ILogger<NoticeCommand> _logger;
        DiscordSocketClient _client;

        public string Name { get; set; } = "notice";
        public string Description { get; set; } = "在指定頻道發出通知";


        public NoticeCommand(ILogger<NoticeCommand> logger, DiscordSocketClient client)
        {
            _logger = logger;
            _client = client;
        }

        public SlashCommandProperties GetSlashCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .AddOption("channel", ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: new List<ChannelType>() { ChannelType.Text })
                .AddOption("content", ApplicationCommandOptionType.String, "內容", isRequired: true, minLength: 1)
                .AddOption("useembed", ApplicationCommandOptionType.Boolean, "使用Embed樣式", isRequired: false)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            ulong[] allowedUser = new ulong[] { 170721070976860161, 664004261998493697 };
            if (!allowedUser.Contains(command.User.Id))
            {
                await command.RespondAsync("你沒有權限使用此指令", ephemeral: true);
                return;
            }
            SocketTextChannel channel = command.Data.Options.First(x => x.Name == "channel").Value as SocketTextChannel;
            string content = command.Data.Options.First(x => x.Name == "content").Value as string;
            bool? useEmbed = command.Data.Options.FirstOrDefault(x => x.Name == "useembed")?.Value as bool?;

            RestUserMessage message;
            if (useEmbed == true)
            {
                SocketGuild guild = _client.GetGuild(command.GuildId.Value);
                SocketGuildUser author = guild.GetUser(command.User.Id);
                message = await channel.SendMessageAsync(embed: new EmbedBuilder().WithTitle($"{author.DisplayName}").WithDescription(content).WithImageUrl(command.User.GetAvatarUrl()).Build());
            }
            else
            {
                message = await channel.SendMessageAsync(content);
            }
            await command.RespondAsync($"{message.GetJumpUrl()}{Environment.NewLine}已經在{channel.Mention}發出通知: {content}", ephemeral: true);
        }
    }
}
