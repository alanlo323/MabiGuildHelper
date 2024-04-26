using Discord;
using Discord.WebSocket;
using DiscordBot.Db.Entity;
using DiscordBot.Db;
using DiscordBot.Helper;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DiscordBot.Configuration;
using Discord.Net;
using Newtonsoft.Json;
using DiscordBot.Extension;
using DiscordBot.Util;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using DiscordBot.ButtonHandler;
using DiscordBot.SelectMenuHandler;
using DiscordBot.MessageHandler;
using Microsoft.EntityFrameworkCore;
using DiscordBot.Migrations;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Linq;
using DiscordBot.Commands.MessageCommand;
using static DiscordBot.Commands.IBaseCommand;
using Quartz.Util;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;

namespace DiscordBot
{
    public class Bot
    {
        ILogger<Bot> logger;
        IServiceProvider serviceProvider;
        DiscordSocketClient client;
        DiscordBotConfig discordBotConfig;
        GameConfig gameConfig;
        AppDbContext appDbContext;
        DatabaseHelper databaseHelper;
        ButtonHandlerHelper buttonHandlerHelper;
        SelectMenuHandlerHelper selectMenuHandlerHelper;
        MessageReceivedHandler messageReceivedHandler;

        bool isReady = false;

        public Bot(
            ILogger<Bot> logger,
            IServiceProvider serviceProvider,
            DiscordSocketClient client,
            IOptionsSnapshot<DiscordBotConfig> discordBotConfig,
            IOptionsSnapshot<GameConfig> gameConfig,
            AppDbContext appDbContext,
            DatabaseHelper databaseHelper,
            ButtonHandlerHelper buttonHandlerHelper,
            SelectMenuHandlerHelper selectMenuHandlerHelper,
            MessageReceivedHandler messageReceivedHandler)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.client = client;
            this.discordBotConfig = discordBotConfig.Value;
            this.gameConfig = gameConfig.Value;
            this.appDbContext = appDbContext;
            this.databaseHelper = databaseHelper;
            this.buttonHandlerHelper = buttonHandlerHelper;
            this.selectMenuHandlerHelper = selectMenuHandlerHelper;
            this.messageReceivedHandler = messageReceivedHandler;

            client.Log += LogAsync;
            client.Ready += ClientReady;
            client.GuildAvailable += ClientGuildAvailable;
            client.SlashCommandExecuted += SlashCommandHandler;
            client.ButtonExecuted += ButtonHandler;
            client.SelectMenuExecuted += MenuHandler;
            client.MessageReceived += MessageHandler;
            client.MessageCommandExecuted += MessageCommandHandler;
            client.ModalSubmitted += ModalSubmittedHandler;
        }

        private Task LogAsync(LogMessage msg)
        {
            logger.Log((msg.Severity) switch
            {
                LogSeverity.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
                LogSeverity.Error => Microsoft.Extensions.Logging.LogLevel.Error,
                LogSeverity.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
                LogSeverity.Info => Microsoft.Extensions.Logging.LogLevel.Information,
                LogSeverity.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
                LogSeverity.Verbose => Microsoft.Extensions.Logging.LogLevel.Trace,
                _ => Microsoft.Extensions.Logging.LogLevel.Information
            }, msg.Exception, msg.Message);
            return Task.CompletedTask;
        }

        public async Task Start()
        {
            await Task.Delay(100);

            logger.LogInformation("Starting Discord Bot");
            logger.LogInformation($"Running in {EnvironmentUtil.GetEnvironment()} mode");

            await Init();

            await client.LoginAsync(TokenType.Bot, EnvironmentUtil.IsProduction() ? discordBotConfig.Token : discordBotConfig.BetaToken);
            await client.StartAsync();
        }

        public async Task Init()
        {
            var guildSettings = await appDbContext.GuildSettings.Where(x => x.CromBasHelperChannelId != null).ToListAsync();
            RuntimeDbUtil.DefaultRuntimeDb[MessageReceivedHandler.CromBasHelperChannelIdListKey] = guildSettings.Select(x => x.CromBasHelperChannelId).ToList();
        }

        public async Task ClientReady()
        {
            logger.LogInformation($"Running in {client.Guilds.Count} servers");
            await client.SetActivityAsync(new Game(gameConfig.DisplayName, ActivityType.Playing));
            //await RefreshCommand();

            isReady = true;
        }

        public async Task ClientGuildAvailable(SocketGuild guild)
        {
            try
            {
                await RefreshCommandForGuild(guild);
                await databaseHelper.GetOrCreateEntityByKeys<GuildSetting>(new Dictionary<string, object>() { { nameof(GuildSetting.GuildId), guild.Id } });
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }

        private async Task RefreshCommandForGuild(SocketGuild guild)
        {
            ApplicationCommandProperties[] slashCommandProperties = serviceProvider
                .GetServices<IBaseSlashCommand>()
                .Where(x => x.Availability == CommandAvailability.Global || (x.Availability == CommandAvailability.AdminServerOnly && guild.Id == ulong.Parse(discordBotConfig.AdminServerId)))
                .Select(x => x.GetCommandProperties())
                .ToArray();
            ApplicationCommandProperties[] messageCommandProperties = serviceProvider
                .GetServices<IBaseMessageCommand>()
                .Where(x => x.Availability == CommandAvailability.Global || (x.Availability == CommandAvailability.AdminServerOnly && guild.Id == ulong.Parse(discordBotConfig.AdminServerId)))
                .Select(x => x.GetCommandProperties())
                .ToArray();

            try
            {
                await guild.BulkOverwriteApplicationCommandAsync([.. slashCommandProperties, .. messageCommandProperties]);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, exception.Message);
                logger.LogWarning($"Cannot refresh commands for Guild: {guild.Name} [{guild.Id}]");
            }
            logger.LogInformation($"Guild:{guild.Name} [{guild.Id}] commands refreshed");
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseSlashCommand instance = serviceProvider.GetServices<IBaseSlashCommand>().Single(x => x.Name == command.CommandName);
                SocketUser user = command.User;
                if (command.IsDMInteraction)
                {
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = client.GetGuild(command.GuildId.Value);
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{command.Channel.Name}({command.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    try
                    {
                        await instance.Excute(command);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);

                        StringBuilder errorMsgBuilder = new();
                        Exception currentException = ex;
                        do
                        {
                            errorMsgBuilder.AppendLine($"{currentException?.Message}");
                            currentException = currentException?.InnerException;
                        } while (currentException != null);
                        logger.LogWarning(ex, errorMsgBuilder.ToString());

                        string errorMsg = $"小幫手發生錯誤, 請聯絡作者{Environment.NewLine}{errorMsgBuilder.ToString().ToQuotation()}";
                        errorMsg = errorMsg[..Math.Min(2000, errorMsg.Length)];

                        try
                        {
                            await command.RespondAsync(errorMsg);
                        }
                        catch
                        {
                            await command.FollowupAsync(errorMsg);
                        }
                    }
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }

        private async Task MessageCommandHandler(SocketMessageCommand command)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseMessageCommand instance = serviceProvider.GetServices<IBaseMessageCommand>().Single(x => x.Name == command.CommandName);
                SocketUser user = command.User;
                if (command.IsDMInteraction)
                {
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = client.GetGuild(command.GuildId.Value);
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{command.Channel.Name}({command.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(command);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }

        private async Task ButtonHandler(SocketMessageComponent component)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseButtonHandler instance = buttonHandlerHelper.GetButtonHandler(component.Data.CustomId);
                SocketUser user = component.User;
                if (component.IsDMInteraction)
                {
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = client.GetGuild(component.GuildId.Value);
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{component.Channel.Name}({component.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(component);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }

        private async Task MenuHandler(SocketMessageComponent component)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseSelectMenuHandler instance = selectMenuHandlerHelper.GetSelectMenuHandler(component.Data.CustomId);
                SocketUser user = component.User;
                if (component.IsDMInteraction)
                {
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = client.GetGuild(component.GuildId.Value);
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{component.Channel.Name}({component.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(component);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }

        private async Task MessageHandler(SocketMessage message)
        {
            try
            {
                Thread newThread = new(async () =>
                {
                    await messageReceivedHandler.Excute(message);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }

        private async Task ModalSubmittedHandler(SocketModal modal)
        {
            try
            {

                while (!isReady) await Task.Delay(100);

                IBaseModalHandler instance = serviceProvider.GetServices<IBaseModalHandler>().Single(x => modal.Data.CustomId.StartsWith(x.CustomId));
                SocketUser user = modal.User;
                if (modal.IsDMInteraction)
                {
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = client.GetGuild(modal.GuildId.Value);
                    logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{modal.Channel.Name}({modal.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Execute(modal);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, ex.Message);
            }
        }
    }
}
