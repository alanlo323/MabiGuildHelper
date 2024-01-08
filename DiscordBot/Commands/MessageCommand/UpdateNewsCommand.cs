using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Extension;

namespace DiscordBot.Commands.MessageCommand
{
    public class UpdateNewsCommand : IBaseMessageCommand
    {
        public string Name { get; set; } = "更新小幫手通知";
        public string Description { get; set; }

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new MessageCommandBuilder()
                .WithName(Name)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                ;
            return command.Build();
        }

        public async Task Excute(SocketMessageCommand command)
        {
        }
    }
}
