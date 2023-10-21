using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands
{
    public class RandomCommand : IBaseCommand
    {
        public string Name { get; set; } = "random";
        public string Description { get; set; } = "Return random result";

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
            await command.RespondAsync($"{Random.Shared.NextDouble()}");
        }
    }
}
