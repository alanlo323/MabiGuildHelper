using Discord;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Constant;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using DiscordBot.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using Quartz;

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
            builder.Services.AddOptions<DiscordBotConfig>().Bind(builder.Configuration.GetSection(DiscordBotConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();
            builder.Services.AddOptions<GameConfig>().Bind(builder.Configuration.GetSection(GameConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();
            builder.Services.AddOptions<ImgurConfig>().Bind(builder.Configuration.GetSection(ImgurConfig.SectionName)).Validate(x => x.Validate()).ValidateOnStart();

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                var config = new LoggingConfiguration(new NLog.LogFactory());
                var logconsole = new ConsoleTarget();
                config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ConsoleTarget());
                config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new FileTarget
                {
                    FileName = builder.Configuration.GetSection(NLogConstant.SectionName).GetValue<string>(NLogConstant.FileName),
                    Layout = builder.Configuration.GetSection(NLogConstant.SectionName).GetValue<string>(NLogConstant.Layout),
                }); ; ;

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
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandHelper>()
                .AddSingleton<DatabaseHelper>()
                .AddSingleton<ImgurHelper>()
                .AddScoped<IBaseCommand, DebugCommand>()
                .AddScoped<IBaseCommand, AboutCommand>()
                .AddScoped<IBaseCommand, HelpCommand>()
                .AddScoped<IBaseCommand, SettingCommand>()
                .AddScoped<IBaseCommand, ErinnTimeCommand>()
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
                            //.StartAt((DateTimeOffset)DateTime.Now)
                            .WithSimpleSchedule(x => x
                                .WithIntervalInHours(24)
                                .RepeatForever()
                            ));

                        q.ScheduleJob<DailyDungeonInfoJob>(trigger => trigger
                            .WithIdentity(DailyDungeonInfoJob.Key.Name)
                            .StartAt((DateTimeOffset)DateTimeUtil.GetNextGivenTime(7, 0, 0))
                            //.StartAt((DateTimeOffset)DateTime.Now)
                            .WithSimpleSchedule(x => x
                                .WithIntervalInHours(24)
                                .RepeatForever()
                            ));
                    }
                })
                .AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; }).AddQuartzHostedService(options =>
                {
                    options.WaitForJobsToComplete = true;
                });

            using IHost host = builder.Build();
            //await host.Services.GetRequiredService<DatabaseHelper>().ResetDatabase();
            await host.Services.GetRequiredService<Bot>().Start();
            host.Run();
        }
    }
}