using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Extension;
using DiscordBot.Util;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class ErinnTimeCommand : IBaseCommand
    {
        public string Name { get; set; } = "erinntime";
        public string Description { get; set; } = "顯示目前愛爾琳時間";

        public SlashCommandProperties GetSlashCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            await command.RespondAsync($"愛爾琳時間⏱ {GameUtil.GetErinnTime():tt h:mm}", ephemeral: true);
        }
    }
}
