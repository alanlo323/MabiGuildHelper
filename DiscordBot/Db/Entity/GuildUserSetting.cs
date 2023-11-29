using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace DiscordBot.Db.Entity
{
    public class GuildUserSetting : BaseEntity
    {
        // Parent
        public GuildSetting GuildSetting { get; set; }

        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }

        // Has children
        public ICollection<InstanceReminderSetting> InstanceReminderSettings { get; set; } = new List<InstanceReminderSetting>();

    }
}
