using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class DebugCommand(ILogger<DebugCommand> logger, ImgurHelper imgurHelper) : IBaseCommand
    {
        public string Name { get; set; } = "debug";
        public string Description { get; set; } = "測試";

        public SlashCommandProperties GetSlashCommandProperties()
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
            await command.DeferAsync();
            await command.FollowupAsync(ephemeral: true, embed: EmbedUtil.GetTodayDungeonInfoEmbed(imgurHelper));
        }
    }
}
