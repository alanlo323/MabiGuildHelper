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
using Quartz;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SchedulerJob
{
    public class ErinnTimeJob : IJob
    {
        public static readonly JobKey Key = new("ErinnTime");

        ILogger<ErinnTimeJob> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;

        public ErinnTimeJob(ILogger<ErinnTimeJob> logger, DiscordSocketClient client, AppDbContext appDbContext)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var applicationInfo = await _client.GetApplicationInfoAsync();
            var guildSettings = _appDbContext.GuildSettings.ToList();
            foreach (GuildSetting guildSetting in guildSettings)
            {
                var guild = _client.GetGuild(guildSetting.GuildId);
                if (guild == null) continue;

                SocketTextChannel textChannel = guild.GetTextChannel(guildSetting.ErinnTimeChannelId ?? 0);
                if (textChannel == null) continue;

                string newName = $"愛爾琳時間";
                if (textChannel.Name != newName)
                {
                    string oldName = textChannel.Name;
                    await textChannel.ModifyAsync(x => x.Name = newName);
                }

                if (guildSetting.ErinnTimeMessageId == null)
                {
                    await CreateNewMessage(guildSetting, textChannel);
                    continue;
                }

                var message = await textChannel.GetMessageAsync(guildSetting.ErinnTimeMessageId ?? 0);
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
                        x.Embed = GameUtil.GetErinnTimeEmbed(true);
                    });
                    continue;
                }

                _logger.LogError("message is not RestUserMessage");
            }
        }

        private async Task CreateNewMessage(GuildSetting guildSetting, SocketTextChannel textChannel)
        {
            var message = await textChannel.SendMessageAsync(embed: GameUtil.GetErinnTimeEmbed(true));
            guildSetting.ErinnTimeMessageId = message.Id;
            await _appDbContext.SaveChangesAsync();
        }
    }
}
