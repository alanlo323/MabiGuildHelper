using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Configuration
{
    public class ReverseProxyConfig
    {
        public const string SectionName = "ReverseProxy";
        public required Main Main { get; set; }
        public required Mapping[] Mappings { get; set; }

        public bool Validate()
        {
            return true;
        }
    }

    public class Main
    {
        public required string Host { get; set; }
        public required int Port { get; set; }
    }

    public class Mapping
    {
        public required string Linkage { get; set; }
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
}
