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

namespace DiscordBot
{
    public class Bot
    {
        ILogger<Bot> _logger;
        IServiceProvider _serviceProvider;
        DiscordSocketClient _client;
        DiscordBotConfig _discordBotConfig;
        GameConfig _gameConfig;
        AppDbContext _appDbContext;
        DatabaseHelper _databaseHelper;
        ButtonHandlerHelper _buttonHandlerHelper;
        SelectMenuHandlerHelper _selectMenuHandlerHelper;
        MessageReceivedHandler _messageReceivedHandler;

        bool isReady = false;

        public Bot(ILogger<Bot> logger, IServiceProvider serviceProvider, DiscordSocketClient client, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, IOptionsSnapshot<GameConfig> gameConfig, AppDbContext appDbContext, DatabaseHelper databaseHelper, ButtonHandlerHelper buttonHandlerHelper, SelectMenuHandlerHelper selectMenuHandlerHelper, MessageReceivedHandler messageReceivedHandler)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _client = client;
            _discordBotConfig = discordBotConfig.Value;
            _gameConfig = gameConfig.Value;
            _appDbContext = appDbContext;
            _databaseHelper = databaseHelper;
            _buttonHandlerHelper = buttonHandlerHelper;
            _selectMenuHandlerHelper = selectMenuHandlerHelper;
            _messageReceivedHandler = messageReceivedHandler;

            _client.Log += LogAsync;
            _client.Ready += Client_Ready;
            _client.GuildAvailable += Client_GuildAvailable;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.ButtonExecuted += ButtonHandler;
            _client.SelectMenuExecuted += MenuHandler;
            _client.MessageReceived += MessageHandler;
            _client.MessageCommandExecuted += MessageCommandHandler;
            _client.ModalSubmitted += ModalSubmittedHandler;
        }

        private Task LogAsync(LogMessage msg)
        {
            _logger.Log((msg.Severity) switch
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

            _logger.LogInformation("Starting Discord Bot");
            _logger.LogInformation($"Running in {EnvironmentUtil.GetEnvironment()} mode");

            await _client.LoginAsync(TokenType.Bot, EnvironmentUtil.IsProduction() ? _discordBotConfig.Token : _discordBotConfig.BetaToken);
            await _client.StartAsync();
        }

        public async Task Client_Ready()
        {
            _logger.LogInformation($"Runngin in {_client.Guilds.Count} servers");
            await _client.SetActivityAsync(new Game(_gameConfig.DisplayName, ActivityType.Playing));
            //await RefreshCommand();

            isReady = true;
        }

        public async Task Client_GuildAvailable(SocketGuild guild)
        {
            try
            {
                await RefreshCommandForGuild(guild);
                await _databaseHelper.GetOrCreateEntityByKeys<GuildSetting>(new Dictionary<string, object>() { { nameof(GuildSetting.GuildId), guild.Id } });
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        private async Task RefreshCommand()
        {
            _logger.LogInformation("Refreshing commands");

            ApplicationCommandProperties[] commandProperties = _serviceProvider.GetServices<IBaseCommand>().Select(x => x.GetCommandProperties()).ToArray();

            foreach (SocketGuild guild in _client.Guilds)
            {
                try
                {
                    await guild.BulkOverwriteApplicationCommandAsync(commandProperties);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, exception.Message);
                    _logger.LogWarning($"Cannot refresh commands for Guild: {guild.Name} [{guild.Id}]");
                }
            }
            _logger.LogInformation("Commands refreshed");
        }

        private async Task RefreshCommandForGuild(SocketGuild guild)
        {
            ApplicationCommandProperties[] slashCommandProperties = _serviceProvider.GetServices<IBaseSlashCommand>().Select(x => x.GetCommandProperties()).ToArray();
            ApplicationCommandProperties[] messageCommandProperties = _serviceProvider.GetServices<IBaseMessageCommand>().Select(x => x.GetCommandProperties()).ToArray();

            try
            {
                await guild.BulkOverwriteApplicationCommandAsync([.. slashCommandProperties, .. messageCommandProperties]);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);
                _logger.LogWarning($"Cannot refresh commands for Guild: {guild.Name} [{guild.Id}]");
            }
            _logger.LogInformation($"Guild:{guild.Name} [{guild.Id}] commands refreshed");
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseSlashCommand instance = _serviceProvider.GetServices<IBaseSlashCommand>().Single(x => x.Name == command.CommandName);
                SocketUser user = command.User;
                if (command.IsDMInteraction)
                {
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = _client.GetGuild(command.GuildId.Value);
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{command.Channel.Name}({command.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(command);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        private async Task MessageCommandHandler(SocketMessageCommand command)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseMessageCommand instance = _serviceProvider.GetServices<IBaseMessageCommand>().Single(x => x.Name == command.CommandName);
                SocketUser user = command.User;
                if (command.IsDMInteraction)
                {
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = _client.GetGuild(command.GuildId.Value);
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{command.Channel.Name}({command.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(command);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        private async Task ButtonHandler(SocketMessageComponent component)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseButtonHandler instance = _buttonHandlerHelper.GetButtonHandler(component.Data.CustomId);
                SocketUser user = component.User;
                if (component.IsDMInteraction)
                {
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = _client.GetGuild(component.GuildId.Value);
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{component.Channel.Name}({component.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(component);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        private async Task MenuHandler(SocketMessageComponent component)
        {
            try
            {
                while (!isReady) await Task.Delay(100);

                IBaseSelectMenuHandler instance = _selectMenuHandlerHelper.GetSelectMenuHandler(component.Data.CustomId);
                SocketUser user = component.User;
                if (component.IsDMInteraction)
                {
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = _client.GetGuild(component.GuildId.Value);
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{component.Channel.Name}({component.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(component);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        private async Task MessageHandler(SocketMessage message)
        {
            try
            {
                Thread newThread = new(async () =>
                {
                    await _messageReceivedHandler.Excute(message);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }

        private async Task ModalSubmittedHandler(SocketModal modal)
        {
            try
            {

                while (!isReady) await Task.Delay(100);

                IBaseModalHandler instance = _serviceProvider.GetServices<IBaseModalHandler>().Single(x => modal.Data.CustomId.StartsWith(x.CustomId));
                SocketUser user = modal.User;
                if (modal.IsDMInteraction)
                {
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} with DM");
                }
                else
                {
                    SocketGuild guild = _client.GetGuild(modal.GuildId.Value);
                    _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {instance.GetType().Name} in [{guild.Name}({guild.Id})] #{modal.Channel.Name}({modal.Channel.Id})");

                }

                Thread newThread = new(async () =>
                {
                    await instance.Excute(modal);
                });
                newThread.Start();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                throw;
            }
        }
    }
}
