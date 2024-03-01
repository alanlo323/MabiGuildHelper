using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.ButtonHandler
{
    public interface IBaseModalHandler
    {
        public string CustomId { get; set; }

        public async Task Execute(SocketModal modal) { }
    }
}
