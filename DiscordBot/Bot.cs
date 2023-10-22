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

namespace DiscordBot
{
    public class Bot
    {
        ILogger<Bot> _logger;
        DiscordSocketClient _client;
        DiscordBotConfig _discordBotConfig;
        GameConfig _gameConfig;
        AppDbContext _appDbContext;
        CommandHelper _commandController;

        bool isReady = false;

        public Bot(ILogger<Bot> logger, DiscordSocketClient client, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, IOptionsSnapshot<GameConfig> gameConfig, AppDbContext appDbContext, CommandHelper commandController)
        {
            _logger = logger;
            _client = client;
            _discordBotConfig = discordBotConfig.Value;
            _gameConfig = gameConfig.Value;
            _appDbContext = appDbContext;
            _commandController = commandController;

            _client.Log += LogAsync;
            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
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

        public async void Start()
        {
            await Task.Delay(100);

            _logger.LogInformation("Starting Discord Bot");

            await _appDbContext.Database.EnsureCreatedAsync();

            await _client.LoginAsync(TokenType.Bot, _discordBotConfig.Token);
            await _client.StartAsync();
        }

        public async Task Client_Ready()
        {
            isReady = true;
            _logger.LogInformation($"Runngin in {_client.Guilds.Count} servers");
            await _client.SetActivityAsync(new Game(_gameConfig.DisplayName, ActivityType.Playing));
            await RefreshCommand();
        }

        private async Task RefreshCommand()
        {
            _logger.LogInformation("Refreshing commands");

            List<SlashCommandProperties> commandProperties = _commandController.GetCommandList().Select(x => x.GetSlashCommandProperties()).ToList();

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

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            if(!isReady) return; ;

            SocketUser user = command.User;
            IBaseCommand commandInstance = _commandController.GetCommand(command.CommandName);
            if (command.IsDMInteraction)
            {
                _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {commandInstance.GetType().Name} with DM");
            }
            else
            {
                SocketGuild guild = _client.GetGuild(command.GuildId.Value);
                _logger.LogInformation($"{user.GlobalName}({user.Username}:{user.Id}) used {commandInstance.GetType().Name} in [{guild.Name}({guild.Id})] #{command.Channel.Name}({command.Channel.Id})");

            }
            //if (command.Data.Options.Count > 0) _logger.LogInformation($"Option: {command.Data.Options.ToJsonString()}");
            await commandInstance.Excute(command);
        }

    }
}
