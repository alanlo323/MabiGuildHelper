using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Extension;
using DiscordBot.Util;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class HelpCommand : IBaseSlashCommand
    {
        public string Name { get; set; } = "help";
        public string Description { get; set; } = "顯示使用指南";
        public CommandAvailability Availability { get; set; } = CommandAvailability.Global;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                ;
            return command.Build();
        }

        public async Task Execute(SocketSlashCommand command)
        {
            await command.RespondAsync("待定, 暫不提供".ToQuotation(), ephemeral: true);
        }
    }
}
