using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Elastic.Clients.Elasticsearch;
using Microsoft.SemanticKernel;

namespace DiscordBot.SemanticKernel.Plugins.About
{
    public class AboutPlugin
    {
        [KernelFunction]
        [Description("Get the current date and time in the local time zone")]
        public string Now()
        {
            return DateTime.Now.ToString("f");
        }

        [KernelFunction]
        [Description("Get the background information about this request likes caller name, location, etc..")]
        public string GetBackgroundInformation(
            [Description("The name of the user.")] string username
            )
        {
            StringBuilder sb = new();
            sb.AppendLine($"BEGIN BACKGROUND INFORMATION:");
            sb.AppendLine($"User Name: {username}");
            sb.AppendLine($"Location: Hong Kong / Taiwan");
            sb.AppendLine($"Current DateTime: {Now()}");
            sb.AppendLine($"END BACKGROUND INFORMATION.");
            return sb.ToString();
        }
    }
}
