using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Db.Entity;

namespace DiscordBot.SelectMenuHandler
{
    public interface IBaseSelectMenuHandler
    {
        public string Id { get; set; }

        public MessageComponent GetMessageComponent(IEnumerable<IReminderSetting> reminderSettings);
        public async Task Excute(SocketMessageComponent component) { }
    }
}
