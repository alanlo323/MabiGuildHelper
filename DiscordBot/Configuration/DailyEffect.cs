using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Configuration
{
    public class DailyEffect
    {
        public string DayOfWeek { get; set; }
        public string ChannelName { get; set; }
        public string Title { get; set; }
        public string[] Effect { get; set; }
    }
}
