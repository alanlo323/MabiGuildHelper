using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Db.Entity
{
    [PrimaryKey(nameof(GuildId))]
    public class GuildSetting
    {
        public ulong GuildId { get; set; }
        public ulong? ErinnTimeChannelId { get; set; }
        public ulong? ErinnTimeMessageId { get; set; }
        public ulong? DailyEffectChannelId { get; set; }
        public ulong? DailyEffectMessageId { get; set; }
        public ulong? DailyDungeonInfoChannelId { get; set; }
        public ulong? DailyDungeonInfoMessageId { get; set; }
    }
}
