using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Helper
{
    public class ConfigHelper
    {
        private static IConfigurationRoot _myConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        public static string? GetValue(string section)
        {
           return  _myConfig.GetSection(section).Value;
        }
    }
}
