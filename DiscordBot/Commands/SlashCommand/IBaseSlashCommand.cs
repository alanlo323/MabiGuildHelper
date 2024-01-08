using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands.SlashCommand
{
    public interface IBaseSlashCommand : IBaseCommand
    {
        public async Task Excute(SocketSlashCommand command) { }
    }
}
