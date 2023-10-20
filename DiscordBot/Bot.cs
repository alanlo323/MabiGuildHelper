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

namespace DiscordBot
{
    public class Bot
    {
        ILogger<Bot> _logger;
        DiscordSocketClient _client;
        DiscordBotConfig _discordBotConfig;

        public Bot(ILogger<Bot> logger, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, AppDbContext appDbContext)
        {
            _logger = logger;
            _discordBotConfig = discordBotConfig.Value;
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;

            appDbContext.Database.EnsureCreated();
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
            _logger.LogInformation("Starting bot");

            await _client.LoginAsync(TokenType.Bot, _discordBotConfig.Token);
            await _client.StartAsync();
        }
    }
}
