using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;

namespace DiscordBot.Commands.SlashCommand
{
    public class SpeakCommand(ILogger<SpeakCommand> logger, DiscordSocketClient client, DatabaseHelper databaseHelper) : IBaseSlashCommand
    {
        public string Name { get; set; } = "speak";
        public string Description { get; set; } = "在指定頻道模仿某個人發言";
        public CommandAvailability Availability { get; set; } = CommandAvailability.Global;

        private string OptionUser = "user";
        private string OptionUsername = "username";
        private string OptionIcon = "icon";
        private string OptionIconUrl = "iconurl";
        private string OptionChannel = "channel";
        private string OptionContent = "content";

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .AddOption(OptionUser, ApplicationCommandOptionType.User, "發言用戶", isRequired: false)
                .AddOption(OptionUsername, ApplicationCommandOptionType.String, "發言用戶名字", isRequired: false)
                .AddOption(OptionIcon, ApplicationCommandOptionType.Attachment, "用戶圖片", isRequired: false)
                .AddOption(OptionIconUrl, ApplicationCommandOptionType.String, "用戶圖片網址", isRequired: false)
                .AddOption(OptionChannel, ApplicationCommandOptionType.Channel, "目標頻道", isRequired: true, channelTypes: [ChannelType.Text])
                .AddOption(OptionContent, ApplicationCommandOptionType.String, "內容", isRequired: true, minLength: 1)
                ;
            return command.Build();
        }

        public async Task Execute(SocketSlashCommand command)
        {
            ulong[] allowedUser = [170721070976860161, 664004261998493697];
            if (!allowedUser.Contains(command.User.Id))
            {
                await command.RespondAsync("你沒有權限使用此指令", ephemeral: true);
                return;
            }

            await command.DeferAsync(ephemeral: true);

            SocketGuildUser optionUser = command.Data.Options.First(x => x.Name == OptionUser).Value as SocketGuildUser;
            string optionUsername = command.Data.Options.First(x => x.Name == OptionUsername).Value as string;
            Attachment optionIcon = command.Data.Options.First(x => x.Name == OptionIcon).Value as Attachment;
            string optionIconUrl = command.Data.Options.First(x => x.Name == OptionIconUrl).Value as string;
            IIntegrationChannel optionChannel = command.Data.Options.First(x => x.Name == OptionChannel).Value as IIntegrationChannel;
            string optionContent = command.Data.Options.First(x => x.Name == OptionContent).Value as string;

            if (optionUser == null && optionUsername == default) throw new Exception("請選擇發言用戶");
            if (optionUser == null && optionIcon == default && optionIconUrl == default) throw new Exception("請選擇用戶圖片或圖片網址");

            SocketGuild guild = client.GetGuild(command.GuildId!.Value);

            string username = string.Empty;
            string avatarUrl = string.Empty;

            if (optionUser != null) username = optionUser.DisplayName;
            if (optionUsername != default) username = optionUsername;

            if (optionUser != default) avatarUrl = optionUser!.GetDisplayAvatarUrl();
            if (optionIconUrl != default) avatarUrl = optionIconUrl;
            if (optionIcon != null) avatarUrl = optionIcon.Url;

            DiscordWebhookClient webhookClient = await GetChannelWebhookClient(guild, optionChannel);
            ulong messageId = await webhookClient.SendMessageAsync(optionContent, username: username, avatarUrl: avatarUrl);

            SocketTextChannel messageChannel = optionChannel as SocketTextChannel;
            IMessage message = await messageChannel!.GetMessageAsync(messageId);
            await command.FollowupAsync($"{message.GetJumpUrl()}{Environment.NewLine}已經在{messageChannel.Mention}模仿{username}發言:{Environment.NewLine}{optionContent}", ephemeral: true);
        }

        private async Task<DiscordWebhookClient> GetChannelWebhookClient(SocketGuild guild, IIntegrationChannel optionChannel)
        {
            IEnumerable<RestWebhook> webhooks = await guild.GetWebhooksAsync();
            IWebhook? webhook = webhooks.FirstOrDefault(x => x.Creator.Id == client.CurrentUser.Id && x.Name == nameof(SpeakCommand) && x.ChannelId == optionChannel.Id);
            if (webhook == null)
            {
                string avatarUrl = client.CurrentUser.GetDisplayAvatarUrl();
                Stream avatarStream = await MiscUtil.GetStreamFromUrl(avatarUrl);
                webhook = await optionChannel!.CreateWebhookAsync(nameof(SpeakCommand), avatar: avatarStream, options: new()
                {
                    AuditLogReason = $"{nameof(SpeakCommand)} 指令",
                    RetryMode = RetryMode.AlwaysRetry,
                });
            }
            DiscordWebhookClient webhookClient = new(webhook);
            return webhookClient;
        }
    }
}
