using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Configuration
{
    public class GameConfig
    {
        public const string SectionName = "Game";

        public string DisplayName { get; set; }
        public DailyEffect[] DailyEffect { get; set; }
        public DailyBankGift[] DailyBankGift { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(DisplayName);
        }
    }

    public class DailyEffect
    {
        public string DayOfWeek { get; set; }
        public string ChannelName { get; set; }
        public string Title { get; set; }
        public string[] Effect { get; set; }
    }

    public class DailyBankGift
    {
        public string DayOfWeek { get; set; }
        public string[] Items { get; set; }
    }
}
