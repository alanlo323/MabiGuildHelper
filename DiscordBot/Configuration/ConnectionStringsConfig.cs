using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Util;

namespace DiscordBot.Configuration
{
    public class ConnectionStringsConfig
    {
        public const string SectionName = "ConnectionStrings";

        public string MabiDb { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(MabiDb);
        }
    }
}
