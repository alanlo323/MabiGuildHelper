using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Extension;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class HelpCommand : IBaseCommand
    {
        public string Name { get; set; } = "help";
        public string Description { get; set; } = "顯示使用指南";

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
            await command.RespondAsync("待定, 暫不提供".ToQuotation(), ephemeral: true);
        }
    }
}
