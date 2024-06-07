using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SelectMenuHandler;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.ButtonHandler
{
    public class ManageReminderButtonHandler(ILogger<ManageReminderButtonHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, SelectMenuHandlerHelper selectMenuHandlerHelper) : IBaseButtonHandler
    {
        public const string InstanceResetReminderButtonLabel = "管理重置提醒";
        public const string InstanceResetReminderButtonId = "ManageInstanceResetReminderButton";
        public const string DailyVipGiftReminderButtonLabel = "管理VIP禮物提醒";
        public const string DailyVipGiftReminderButtonIdId = "ManageDailyVipGiftReminderButton";

        public string[] Lables { get; set; } = [InstanceResetReminderButtonLabel, DailyVipGiftReminderButtonLabel];
        public string[] Ids { get; set; } = [InstanceResetReminderButtonId, DailyVipGiftReminderButtonIdId];

        public MessageComponent GetMessageComponent()
        {
            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton(label: InstanceResetReminderButtonLabel, emote: new Emoji("⚙️"), style: ButtonStyle.Primary, customId: InstanceResetReminderButtonId)
                .WithButton(label: DailyVipGiftReminderButtonLabel, emote: new Emoji("⚙️"), style: ButtonStyle.Primary, customId: DailyVipGiftReminderButtonIdId)
                ;
            return componentBuilder.Build();
        }

        public async Task Excute(SocketMessageComponent component)
        {
            var userSetting = await databaseHelper.GetOrCreateEntityByKeys<GuildUserSetting>(new() { { nameof(GuildUserSetting.GuildId), component.GuildId }, { nameof(GuildUserSetting.UserId), component.User.Id } }, includeProperties: [nameof(GuildUserSetting.InstanceReminderSettings), nameof(GuildUserSetting.DailyVipGiftReminderSettings)]);
            MessageComponent messageComponent = component.Data.CustomId switch
            {
                InstanceResetReminderButtonId => selectMenuHandlerHelper.GetSelectMenuHandler<AddInstanceResetReminderSelectMenuHandler>().GetMessageComponent(userSetting.InstanceReminderSettings),
                DailyVipGiftReminderButtonIdId => selectMenuHandlerHelper.GetSelectMenuHandler<AddDailyVipGiftReminderSelectMenuHandler>().GetMessageComponent(userSetting.DailyVipGiftReminderSettings),
                _ => throw new NotImplementedException($"{component.Data.CustomId} is not supported by this Handle."),
            };
            await component.RespondAsync(text: "請選擇想要接收的提醒", ephemeral: true, components: messageComponent);
        }
    }
}
