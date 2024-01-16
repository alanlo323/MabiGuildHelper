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
        public string Key { get; set; }
        public string? StringValue { get; set; }
        public int? IntValue { get; set; }
        public ulong? UlongValue { get; set; }
        public double? DoubleValue { get; set; }
        public DateTime? DateTimeValue { get; set; }
    }
}
