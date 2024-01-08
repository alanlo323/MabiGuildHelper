using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.Commands.MessageCommand
{
    public interface IBaseMessageCommand : IBaseCommand
    {
        public async Task Excute(SocketMessageCommand command) { }
    }
}
