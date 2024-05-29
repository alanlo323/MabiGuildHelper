using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Util;

namespace DiscordBot.Configuration
{
    public class DiscordBotConfig
    {
        public const string SectionName = "DiscordBot";

        public string ProductionToken { get; set; }
        public string BetaToken { get; set; }
        public string AdminId { get; set; }
        public string AdminServerId { get; set; }

        public string Token { get => EnvironmentUtil.IsProduction() ? ProductionToken : BetaToken; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(AdminServerId);
        }
    }
}
