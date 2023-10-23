using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SchedulerJob
{
    public class DailyEffectJob : IJob
    {
        public static readonly JobKey Key = new("DailyEffectJob");

        ILogger<DailyEffectJob> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        GameConfig _gameConfig;

        public DailyEffectJob(ILogger<DailyEffectJob> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _gameConfig = gameConfig.Value;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var applicationInfo = await _client.GetApplicationInfoAsync();
            var guildSettings = _appDbContext.GuildSettings.ToList();
            foreach (GuildSetting guildSetting in guildSettings)
            {
                var guild = _client.GetGuild(guildSetting.GuildId);
                if (guild == null) continue;

                SocketTextChannel textChannel = guild.GetTextChannel(guildSetting.DailyEffectChannelId ?? 0);
                if (textChannel == null) continue;

                var today = DateTime.Now.DayOfWeek.ToString();
                var todayEffect = _gameConfig.DailyEffect.First(x => x.DayOfWeek == today);
                string newName = $"{todayEffect.ChannelName}";
                if (textChannel.Name != newName)
                {
                    string oldName = textChannel.Name;
                    await textChannel.ModifyAsync(x => x.Name = newName);
                }

                if (guildSetting.DailyEffectMessageId == null)
                {
                    await CreateNewMessage(guildSetting, textChannel);
                    continue;
                }

                var message = await textChannel.GetMessageAsync(guildSetting.DailyEffectMessageId ?? 0);
                if (message == null)
                {
                    await CreateNewMessage(guildSetting, textChannel);
                    continue;
                };

                if (message.Author.Id != applicationInfo.Id)
                {
                    await CreateNewMessage(guildSetting, textChannel);
                    continue;
                }

                if (message is RestUserMessage userMessage)
                {
                    await userMessage.ModifyAsync(x =>
                    {
                        x.Embed = GameUtil.GetDailyEffectEmbed(_gameConfig);
                    });
                    continue;
                }

                _logger.LogError("message is not RestUserMessage");
            }
        }

        private async Task CreateNewMessage(GuildSetting guildSetting, SocketTextChannel textChannel)
        {
            var message = await textChannel.SendMessageAsync(embed: GameUtil.GetDailyEffectEmbed(_gameConfig));
            guildSetting.DailyEffectMessageId = message.Id;
            await _appDbContext.SaveChangesAsync();
        }
    }
}
