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
    public class GlobalSetting : BaseEntity
    {
        public ulong GuildId { get; set; }
        public ulong? AttachmentChannelId { get; set; }
    }
}
