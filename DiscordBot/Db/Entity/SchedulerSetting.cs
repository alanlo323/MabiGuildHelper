using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Db.Entity
{
    [PrimaryKey(nameof(Id))]
    public class SchedulerSetting
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
    }
}
