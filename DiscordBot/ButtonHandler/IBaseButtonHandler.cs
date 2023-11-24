using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.ButtonHandler
{
    public interface IBaseButtonHandler
    {
        public string Lable { get; set; }
        public string Id { get; set; }

        public MessageComponent GetMessageComponent();
        public async Task Excute(SocketMessageComponent component) { }
    }
}
