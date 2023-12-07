using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Configuration
{
    public class FunnyResponseConfig
    {
        public const string SectionName = "FunnyResponse";

        public string[] TriggerWords { get; set; }
        public string[] TriggerStickers { get; set; }

        public bool Validate()
        {
            return true;
        }
    }
}
