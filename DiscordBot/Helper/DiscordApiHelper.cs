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
using DiscordBot.Migrations;

namespace DiscordBot.Helper
{
    public class DiscordApiHelper(ILogger<DiscordApiHelper> logger, DiscordSocketClient client, AppDbContext appDbContext)
    {
        public async Task UpdateOrCreateMeesage(GuildSetting guildSetting, string channelIdPropertyName, string messageIdPropertyName, string channelName = null, string content = null, string filePath = null, Embed embed = null, MessageComponent messageComponent = null)
        {
            var guild = client.GetGuild(guildSetting.GuildId);
            if (guild == null) return;

            var channelId = guildSetting.GetProperty<ulong?>(channelIdPropertyName);
            if (!channelId.HasValue) return;

            SocketTextChannel textChannel = guild.GetTextChannel(channelId.Value);
            if (textChannel == null) return;

            await textChannel.EnsureChannelName(channelName);

            if (!guildSetting.GetProperty<ulong?>(channelIdPropertyName).HasValue)
            {
                await CreateNewMessage(guildSetting, textChannel, messageIdPropertyName, content: content, filePath: filePath, embed: embed, messageComponent: messageComponent);
                return;
            }

            var messageId = guildSetting.GetProperty<ulong?>(messageIdPropertyName);
            var message = messageId.HasValue ? await textChannel.GetMessageAsync((ulong)messageId) : null;
            var applicationInfo = await client.GetApplicationInfoAsync();
            if (!messageId.HasValue || message == null || message.Author.Id != applicationInfo.Id)
            {
                await CreateNewMessage(guildSetting, textChannel, messageIdPropertyName, content: content, filePath: filePath, embed: embed, messageComponent: messageComponent);
                return;
            }

            if (message is RestUserMessage userMessage)
            {
                await userMessage.ModifyAsync(x =>
                {
                    x.Content = content;
                    x.Embed = embed;
                    x.Components = messageComponent;
                    if (!string.IsNullOrEmpty(filePath)) x.Attachments = new List<FileAttachment>() { new(filePath) };
                });
                return;
            }

            logger.LogError("message is not RestUserMessage");
        }

        public async Task CreateNewMessage(BaseEntity entity, SocketTextChannel textChannel, string messageIdPropertyName, string content = null, string filePath = null, Embed embed = null, MessageComponent messageComponent = null)
        {
            RestUserMessage message;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                message = await textChannel.SendMessageAsync(text: content, embed: embed, components: messageComponent);
            }
            else
            {
                message = await textChannel.SendFileAsync(filePath: filePath, text: content, embed: embed, components: messageComponent);
            }
            entity.SetProperty(messageIdPropertyName, message.Id);
            await appDbContext.SaveChangesAsync();
        }

        public async Task<RestUserMessage?> SendMessage(ulong? guildId, ulong? textChannelId, string content = null, Embed embed = null, MessageComponent messageComponent = null)
        {
            var guild = client.GetGuild(guildId ?? 0);
            if (guild == null) return null;

            SocketTextChannel textChannel = guild.GetTextChannel(textChannelId ?? 0);
            if (textChannel == null) return null;

            return await textChannel.SendMessageAsync(text: content, embed: embed, components: messageComponent);
        }

        public async Task<RestUserMessage?> SendFile(ulong? guildId, ulong? textChannelId, string filePath, string content = null, Embed embed = null, MessageComponent messageComponent = null)
        {
            var guild = client.GetGuild(guildId ?? 0);
            if (guild == null) return null;

            SocketTextChannel textChannel = guild.GetTextChannel(textChannelId ?? 0);
            if (textChannel == null) return null;

            return await textChannel.SendFileAsync(filePath: filePath, text: content, embed: embed, components: messageComponent);
        }

        public async Task<string?> UploadAttachment(string filePath, string sourceFunction)
        {
            try
            {
                var createNewAttachmentResult = await userMessage.Channel.SendFileAsync(filePath, text: $"New attachment from {sourceFunction}");
                return createNewAttachmentResult.Attachments.Single().Url.Split("?")[0];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return null;
            }
        }
    }
}
