using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordBot.ButtonHandler
{
    public interface IBaseAutocompleteHandler
    {
        public string CommandName { get; set; }
        public async Task Excute(SocketAutocompleteInteraction interaction) { }
    }
}
