using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.DataEntity;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SchedulerJob
{
    public class DailyDungeonInfoJob : IJob
    {
        public static readonly JobKey Key = new("DailyDungeonInfoJob");

        ILogger<DailyDungeonInfoJob> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        ImgurHelper _imgurHelper;

        public DailyDungeonInfoJob(ILogger<DailyDungeonInfoJob> logger, DiscordSocketClient client, AppDbContext appDbContext, ImgurHelper imgurHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _imgurHelper = imgurHelper;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Embed ember = EmbedUtil.GetTodayDungeonInfoEmbed(_imgurHelper, out DailyDungeonInfo todayDungeonInfo);
            var applicationInfo = await _client.GetApplicationInfoAsync();
            var guildSettings = _appDbContext.GuildSettings.ToList();
            foreach (GuildSetting guildSetting in guildSettings)
            {
                var guild = _client.GetGuild(guildSetting.GuildId);
                if (guild == null) continue;

                SocketTextChannel textChannel = guild.GetTextChannel(guildSetting.DailyDungeonInfoChannelId ?? 0);
                if (textChannel == null) continue;

                string newName = $"今日老手-{todayDungeonInfo.Name}";
                if (textChannel.Name != newName)
                {
                    string oldName = textChannel.Name;
                    await textChannel.ModifyAsync(x => x.Name = newName);
                }

                if (guildSetting.DailyDungeonInfoMessageId == null)
                {
                    await CreateNewMessage(guildSetting, textChannel, ember);
                    continue;
                }

                var message = await textChannel.GetMessageAsync(guildSetting.DailyDungeonInfoMessageId ?? 0);
                if (message == null)
                {
                    await CreateNewMessage(guildSetting, textChannel, ember);
                    continue;
                };

                if (message.Author.Id != applicationInfo.Id)
                {
                    await CreateNewMessage(guildSetting, textChannel, ember);
                    continue;
                }

                if (message is RestUserMessage userMessage)
                {
                    await userMessage.ModifyAsync(x =>
                    {
                        x.Embed = ember;
                    });
                    continue;
                }

                _logger.LogError("message is not RestUserMessage");
            }
        }

        private async Task CreateNewMessage(GuildSetting guildSetting, SocketTextChannel textChannel, Embed ember)
        {
            var message = await textChannel.SendMessageAsync(embed: ember);
            guildSetting.DailyDungeonInfoMessageId = message.Id;
            await _appDbContext.SaveChangesAsync();
        }
    }
}
