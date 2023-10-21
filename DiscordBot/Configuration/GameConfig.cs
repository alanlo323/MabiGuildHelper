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

        public bool Validate()
        {
            return !string.IsNullOrEmpty(DisplayName);
        }
    }
}
