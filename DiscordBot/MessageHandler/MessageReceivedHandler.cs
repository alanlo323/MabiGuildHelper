using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBot.MessageHandler
{
    public class MessageReceivedHandler
    {
        public async Task Excute(SocketMessage message)
        {
            string[] keyWords = new[] { "test" };
            if (keyWords.Any(x => message.Content.ToLower().Contains(x.ToLower())))
            {
                await message.Channel.SendMessageAsync("hahaha", messageReference: message.Reference);
            }
        }
    }
}
