﻿using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;

namespace DiscordBot
{
    public class Program
    {
        public static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

        public Program()
        {
        }

        public async Task RunAsync(string[] args)
        {

            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddOptions<DiscordBotConfig>().Bind(builder.Configuration.GetSection(DiscordBotConfig.SectionName)).Validate(x => true || x.Validate()).ValidateOnStart();

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                var config = new LoggingConfiguration(new NLog.LogFactory());
                var logconsole = new ConsoleTarget();
                config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ConsoleTarget());
                config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new FileTarget
                {
                    FileName = builder.Configuration.GetSection(NLogConfig.SectionName).GetValue<string>(NLogConfig.FileName),
                    Layout = builder.Configuration.GetSection(NLogConfig.SectionName).GetValue<string>(NLogConfig.Layout),
                }); ; ;

                loggingBuilder.AddNLog(config);
            });

            builder.Services
                .AddSingleton<Bot>()
                .AddSingleton<DiscordSocketClient>()
                .AddDbContext<AppDbContext>(optionsBuilder =>
                {
                    if (!optionsBuilder.IsConfigured)
                    {
                        optionsBuilder.UseSqlite(builder.Configuration.GetConnectionString(AppDbContext.ConnectionStringName));
                    }
                })
                ;

            using IHost host = builder.Build();
            host.Start();

            host.Services.GetRequiredService<Bot>().Start();

            Thread.Sleep(-1);

            await host.StopAsync();
        }

    }
}