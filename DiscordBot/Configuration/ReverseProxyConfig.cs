using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace DiscordBot.Configuration
{
    public class ReverseProxyConfig
    {
        public const string SectionName = "ReverseProxy";

        public required IReadOnlyList<RouteConfig> Routes { get; set; }
        public required IReadOnlyList<ClusterConfig> Clusters { get; set; }
        public required Mapping[] Mappings { get; set; }

        public bool Validate()
        {
            return Routes?.Count > 0 && Clusters.Count > 0;
        }
    }

    public class Mapping
    {
        public required string Name { get; set; }
        public required int Port { get; set; }
    }
}
