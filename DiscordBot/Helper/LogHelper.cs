using NLog.Config;
using NLog.Targets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Helper
{
    public class LogHelper
    {
        public static void CreateLogger()
        {
            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget
            {
                FileName = "${basedir}/logs/${shortdate}.log",
                Layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss} [${uppercase:${level}}] ${message}",
            };
            var logconsole = new ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            LogManager.Configuration = config;
        }
    }
}
