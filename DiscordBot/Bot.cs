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
using DiscordBot.Commands;
using DiscordBot.Extension;
using DiscordBot.Util;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using DiscordBot.ButtonHandler;
using DiscordBot.SelectMenuHandler;
using DiscordBot.MessageHandler;
using Microsoft.EntityFrameworkCore;
using DiscordBot.Migrations;

namespace DiscordBot
{
    public class Bot
    {
        ILogger<Bot> _logger;
        DiscordSocketClient _client;
        DiscordBotConfig _discordBotConfig;
        GameConfig _gameConfig;
        AppDbContext _appDbContext;
        CommandHelper _commandHelper;
        ButtonHandlerHelper _buttonHandlerHelper;
        SelectMenuHandlerHelper _selectMenuHandlerHelper;
        MessageReceivedHandler _messageReceivedHandler;

        bool isReady = false;

        public Bot(ILogger<Bot> logger, DiscordSocketClient client, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, IOptionsSnapshot<GameConfig> gameConfig, AppDbContext appDbContext, CommandHelper commandHelper, ButtonHandlerHelper buttonHandlerHelper, SelectMenuHandlerHelper selectMenuHandlerHelper, MessageReceivedHandler messageReceivedHandler)
        {
            _logger = logger;
            _client = client;
            _discordBotConfig = discordBotConfig.Value;
            _gameConfig = gameConfig.Value;
            _appDbContext = appDbContext;
            _commandHelper = commandHelper;
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
            await RefreshCommandForGuild(guild);
        }

        private async Task RefreshCommand()
        {
            _logger.LogInformation("Refreshing commands");

            List<SlashCommandProperties> commandProperties = _commandHelper.GetCommandList().Select(x => x.GetSlashCommandProperties()).ToList();

            foreach (SocketGuild guild in _client.Guilds)
            {
                try
                {
                    await guild.BulkOverwriteApplicationCommandAsync(commandProperties.ToArray());
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
            List<SlashCommandProperties> commandProperties = _commandHelper.GetCommandList().Select(x => x.GetSlashCommandProperties()).ToList();

            try
            {
                await guild.BulkOverwriteApplicationCommandAsync(commandProperties.ToArray());
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
            while (!isReady) await Task.Delay(100);

            IBaseCommand instance = _commandHelper.GetCommand(command.CommandName);
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

        private async Task ButtonHandler(SocketMessageComponent component)
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
            await instance.Excute(component);
        }

        private async Task MenuHandler(SocketMessageComponent component)
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
            await instance.Excute(component);
        }

        private async Task MessageHandler(SocketMessage message)
        {
            MessageReceivedHandler handler = _messageReceivedHandler;
            await handler.Excute(message);
        }

    }
}
