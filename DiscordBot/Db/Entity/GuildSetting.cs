using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Db.Entity
{
    public class GuildSetting : BaseEntity
    {
        public ulong GuildId { get; set; }
        public ulong? ErinnTimeChannelId { get; set; }
        public ulong? ErinnTimeMessageId { get; set; }
        public ulong? DailyEffectChannelId { get; set; }
        public ulong? DailyEffectMessageId { get; set; }
        public ulong? DailyDungeonInfoChannelId { get; set; }
        public ulong? DailyDungeonInfoMessageId { get; set; }
        public ulong? InstanceResetReminderChannelId { get; set; }
        public ulong? InstanceResetReminderMessageIdBattle { get; set; }
        public ulong? InstanceResetReminderMessageIdLife { get; set; }
        public ulong? InstanceResetReminderMessageIdMisc { get; set; }
        public ulong? InstanceResetReminderMessageIdOneDay { get; set; }
        public ulong? InstanceResetReminderMessageIdToday { get; set; }
        public ulong? DataScapingNewsChannelId { get; set; }
        public ulong? CromBasHelperChannelId { get; set; }
        public ulong? LogChannelId { get; set; }

        public ICollection<GuildUserSetting> GuildUserSettings { get; set; } = [];
        public ICollection<GuildNewsOverride> GuildNewsOverrides { get; set; } = [];
    }
}
