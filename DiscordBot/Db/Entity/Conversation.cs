using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Helper;
using DiscordBot.Migrations;
using DiscordBot.Util;

namespace DiscordBot.Db.Entity
{

    public class Conversation : BaseEntity
    {
        public int Id { get; set; }
        public ulong DiscordMessageId { get; set; }
        public string? UserPrompt { get; set; }
        public string? PlanTemplate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Result { get; set; }

        [NotMapped]
        public TimeSpan? RunningTime { get => EndTime - StartTime; }
    }
}
