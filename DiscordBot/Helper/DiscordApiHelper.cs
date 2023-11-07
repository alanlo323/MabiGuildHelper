using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using Discord;
using DiscordBot.Db.Entity;
using DiscordBot.Db;
using DiscordBot.Extension;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Helper
{
    public class DiscordApiHelper
    {
        private ILogger<DiscordApiHelper> _logger;
        DiscordSocketClient _client;
        private AppDbContext _appDbContext;

        public DiscordApiHelper(ILogger<DiscordApiHelper> logger, DiscordSocketClient client, AppDbContext appDbContext)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
        }

        public async Task UpdateOrCreateMeesage(GuildSetting guildSetting, string channelName, string channelIdPropertyName, string messageIdPropertyName, string content, Embed embed)
        {
            var guild = _client.GetGuild(guildSetting.GuildId);
            if (guild == null) return;

            var channelId = guildSetting.GetProperty<ulong?>(channelIdPropertyName);
            if (!channelId.HasValue) return;

            SocketTextChannel textChannel = guild.GetTextChannel(channelId.Value);
            if (textChannel == null) return;

            await textChannel.EnsureChannelName(channelName);

            if (!guildSetting.GetProperty<ulong?>(channelIdPropertyName).HasValue)
            {
                await CreateNewMessage(guildSetting, textChannel, content, embed);
                return;
            }

            var messageId = guildSetting.GetProperty<ulong?>(messageIdPropertyName);
            var message = messageId.HasValue ? await textChannel.GetMessageAsync((ulong)messageId) : null;
            var applicationInfo = await _client.GetApplicationInfoAsync();
            if (!messageId.HasValue || message == null || message.Author.Id != applicationInfo.Id)
            {
                await CreateNewMessage(guildSetting, textChannel, content, embed);
                return;
            }

            if (message is RestUserMessage userMessage)
            {
                await userMessage.ModifyAsync(x =>
                {
                    x.Content = content;
                    x.Embed = embed;
                });
                return;
            }

            _logger.LogError("message is not RestUserMessage");
        }

        private async Task CreateNewMessage(GuildSetting guildSetting, SocketTextChannel textChannel, string content, Embed embed)
        {
            var message = await textChannel.SendMessageAsync(text: content, embed: embed);
            guildSetting.ErinnTimeMessageId = message.Id;
            await _appDbContext.SaveChangesAsync();
        }
    }
}
