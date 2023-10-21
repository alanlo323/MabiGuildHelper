using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class HelloWorldCommand : IBaseCommand
    {
        public string Name { get; set; } = "helloworld";
        public string Description { get; set; } = "This command does nothing";

        public SlashCommandProperties GetSlashCommandProperties()
        {
            var command = new SlashCommandBuilder();
                .WithName(Name)
                .WithDescription(Description)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            await command.RespondAsync($"You executed {command.Data.Name}");
        }
    }
}
