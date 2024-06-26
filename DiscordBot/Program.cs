﻿using Discord;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.Commands.MessageCommand;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Constant;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.MessageHandler;
using DiscordBot.SchedulerJob;
using DiscordBot.SelectMenuHandler;
using DiscordBot.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Quartz;
using System.Data;
using System.Text.RegularExpressions;

namespace DiscordBot
{
    public partial class Program
    {
        public static void Main(string[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

        public Program()
        {
        }

        public async Task RunAsync(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.AddOptions<DiscordBotConfig>().Bind(builder.Configuration.GetSection(DiscordBotConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();
            builder.Services.AddOptions<GameConfig>().Bind(builder.Configuration.GetSection(GameConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();
            builder.Services.AddOptions<ImgurConfig>().Bind(builder.Configuration.GetSection(ImgurConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();
            builder.Services.AddOptions<FunnyResponseConfig>().Bind(builder.Configuration.GetSection(FunnyResponseConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                IConfigurationSection section = builder.Configuration.GetSection(NLogConstant.SectionName);
                var config = new LoggingConfiguration(new NLog.LogFactory());
                config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ConsoleTarget());
                config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new FileTarget
                {
                    FileName = section.GetValue<string>(NLogConstant.FileName),
                    Layout = section.GetValue<string>(NLogConstant.Layout),
                });

                loggingBuilder.AddNLog(config);
            });

            builder.Services
                .AddDbContext<AppDbContext>(optionsBuilder =>
                {
                    if (!optionsBuilder.IsConfigured)
                    {
                        optionsBuilder.UseSqlite(builder.Configuration.GetConnectionString(AppDbContext.ConnectionStringName));
                    }
                })
                .AddSingleton<Bot>()
                .AddSingleton<ButtonHandlerHelper>()
                .AddSingleton<DatabaseHelper>()
                .AddSingleton<DiscordSocketClient>(x => new(new DiscordSocketConfig { GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent }))
                .AddSingleton<DiscordApiHelper>()
                .AddSingleton<ImgurHelper>()
                .AddSingleton<SelectMenuHandlerHelper>()
                .AddSingleton<DataScrapingHelper>()
                .AddSingleton<ConcurrentRandomHelper>()
                .AddScoped<IBaseSlashCommand, DebugCommand>()
                .AddScoped<IBaseSlashCommand, AboutCommand>()
                .AddScoped<IBaseSlashCommand, HelpCommand>()
                .AddScoped<IBaseSlashCommand, SettingCommand>()
                .AddScoped<IBaseSlashCommand, ErinnTimeCommand>()
                .AddScoped<IBaseSlashCommand, NoticeCommand>()
                .AddScoped<IBaseSlashCommand, AdminCommand>()
                .AddScoped<IBaseSlashCommand, LuckyChannelCommand>()
                .AddScoped<IBaseMessageCommand, EditNewsCommand>()
                .AddScoped<IBaseButtonHandler, ManageReminderButtonHandler>()
                .AddScoped<IBaseSelectMenuHandler, AddReminderSelectMenuHandler>()
                .AddScoped<IBaseModalHandler, EditNewsModalHandler>()
                .AddScoped<MessageReceivedHandler>()
                .AddScoped<DailyDungeonInfoJob>()
                .AddScoped<DailyEffectJob>()
                .AddScoped<ErinnTimeJob>()
                .AddScoped<InstanceResetReminderJob>()
                .AddScoped<DataScrapingJob>()
                ;

            builder.Services
                .AddQuartz(q =>
                {
                    q.UseSimpleTypeLoader();
                    q.UseInMemoryStore();
                    q.UseDefaultThreadPool(tp =>
                    {
                        tp.MaxConcurrency = 10;
                    });

                    if (EnvironmentUtil.IsProduction())
                    {
                        q.ScheduleJob<ErinnTimeJob>(trigger => trigger
                            .WithIdentity(ErinnTimeJob.Key.Name)
                            .StartAt(DateBuilder.NextGivenSecondDate(DateTime.Now, 15))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInSeconds(15)
                                .RepeatForever()
                            ));

                        q.ScheduleJob<DailyEffectJob>(trigger => trigger
                            .WithIdentity(DailyEffectJob.Key.Name)
                            .StartAt((DateTimeOffset)DateTimeUtil.GetNextGivenTime(0, 0, 0))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInHours(24)
                                .RepeatForever()
                            ));

                        q.ScheduleJob<DailyDungeonInfoJob>(trigger => trigger
                            .WithIdentity(DailyDungeonInfoJob.Key.Name)
                            .StartAt((DateTimeOffset)DateTimeUtil.GetNextGivenTime(7, 0, 0))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInHours(24)
                                .RepeatForever()
                            ));

                        q.ScheduleJob<InstanceResetReminderJob>(trigger => trigger
                            .WithIdentity(InstanceResetReminderJob.Key.Name)
                            .StartAt((DateTimeOffset)DateTime.Today.AddHours(DateTime.Now.Hour + 1))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInHours(1)
                                .RepeatForever()
                            ));

                        q.ScheduleJob<DataScrapingJob>(trigger => trigger
                            .WithIdentity(DataScrapingJob.Key.Name)
                            .StartAt(DateBuilder.NextGivenMinuteDate(DateTime.Now, 1))
                            .WithSimpleSchedule(x => x
                                .WithIntervalInMinutes(1)
                                .RepeatForever()
                            ));
                    }
                })
                .AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; }).AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });

            using IHost host = builder.Build();
            await host.Services.GetRequiredService<DatabaseHelper>().EnsureDatabaseReady();
            await host.Services.GetRequiredService<Bot>().Start();
            host.Run();
        }
    }
}