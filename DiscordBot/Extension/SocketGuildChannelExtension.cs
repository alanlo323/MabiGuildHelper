using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBot.Db.Entity;
using DiscordBot.Db;
using DiscordBot.Util;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Extension
{
    public static class SocketGuildChannelExtension
    {
        public async static Task EnsureChannelName(this SocketGuildChannel channel, string targetName)
        {
            if (channel.Name != targetName)
            {
                await channel.ModifyAsync(x => x.Name = targetName);
            }
        }
    }
}
