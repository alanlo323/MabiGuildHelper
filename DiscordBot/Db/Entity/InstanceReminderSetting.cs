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
    [PrimaryKey(nameof(GuildId), nameof(UserId), nameof(InstanceReminderId))]
    public class InstanceReminderSetting : BaseEntity
    {
        [ForeignKey($"{nameof(GuildId)}, {nameof(UserId)}")]
        public GuildUserSetting GuildUserSetting { get; set; }

        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public int InstanceReminderId { get; set; }
    }
}
