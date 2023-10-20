using Discord;
using Discord.WebSocket;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Bot
    {
        Logger _logger;
        DiscordSocketClient _client;
        string _token;

        public Bot(string token)
        {
            _logger = LogManager.GetCurrentClassLogger();

            _token = token;
            _client = new DiscordSocketClient();
            _client.Log += Log;
        }

        private Task Log(LogMessage msg)
        {
            _logger.Info(msg);
            return Task.CompletedTask;
        }

        public async void Start()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();
        }
    }
}
