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

        public required string ProductionToken { get; set; }
        public required string BetaToken { get; set; }
        public required string AdminId { get; set; }
        public required string AdminServerId { get; set; }

        public string Token { get => EnvironmentUtil.IsProduction() ? ProductionToken : BetaToken; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(AdminServerId);
        }
    }
}
