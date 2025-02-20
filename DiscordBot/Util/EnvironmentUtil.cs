using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Util
{
    public class EnvironmentUtil
    {
        public static string? GetEnvironment()
        {
            return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        }

        public static bool IsLocal()
        {
            return GetEnvironment() == "Local";
        }

        public static bool IsDevelopment()
        {
            return GetEnvironment() == "Development";
        }

        public static bool IsProduction()
        {
            return GetEnvironment() == "Production";
        }
    }
}
