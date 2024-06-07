using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SelectMenuHandler
{
    public class AddDailyVipGiftReminderSelectMenuHandler(ILogger<AddDailyVipGiftReminderSelectMenuHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, IOptionsSnapshot<GameConfig> gameConfig) : IBaseSelectMenuHandler
    {
        GameConfig _gameConfig = gameConfig.Value;

        public string Id { get; set; } = "AddDailyGiftReminderSelectMenu";

        public MessageComponent GetMessageComponent(IEnumerable<IReminderSetting> reminderSettings)
        {
            List<DailyVipGift> giftList = [.. _gameConfig.DailyVipGift.OrderBy(x => x.DayOfWeek)];
            if (giftList.Count == 0) throw new NotSupportedException("Empty list in InstanceReset, please check config.");

            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("按我選擇提醒")
                .WithCustomId(Id)
                ;

            foreach (var gift in giftList)
            {
                menuBuilder.AddOption($"{gift.Items.Aggregate((s1, s2) => $"{s1}, {s2}")}", $"{gift.Id}", description: $"可領取日期: {DateTimeFormatInfo.CurrentInfo.GetDayName(gift.DayOfWeek)}", isDefault: (reminderSettings as IEnumerable<DailyVipGiftReminderSetting>)?.Any(x => x.ReminderId == gift.Id));
            }
            menuBuilder
                .WithMinValues(0)
                .WithMaxValues(giftList.Count)
                ;

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            return builder.Build();
        }

        public async Task Excute(SocketMessageComponent component)
        {
            List<DailyVipGift> selectedItems = _gameConfig.DailyVipGift.Where(x => component.Data.Values.Contains($"{x.Id}")).ToList();
            IEnumerable<DailyVipGiftReminderSetting> existingSettings = [.. appDbContext.DailyVipGiftReminderSettings.Where(x => x.GuildId == component.GuildId && x.UserId == component.User.Id)];
            IEnumerable<DailyVipGiftReminderSetting> nonSelectedSettings = existingSettings.Where(x => !selectedItems.Any(y => y.Id == x.ReminderId)).ToList();

            foreach (var item in selectedItems)
            {
                await databaseHelper.GetOrCreateEntityByKeys<DailyVipGiftReminderSetting>(
                    new() {
                        { nameof(DailyVipGiftReminderSetting.GuildId), component.GuildId },
                        { nameof(DailyVipGiftReminderSetting.UserId), component.User.Id },
                        { nameof(DailyVipGiftReminderSetting.ReminderId), item.Id },
                    });
            }

            appDbContext.RemoveRange(nonSelectedSettings);
            await appDbContext.SaveChangesAsync();

            string text = selectedItems.Count > 0
                ? $"設定已更新, 小幫手會在下方的禮物可領取時通知你喔! {Environment.NewLine}> {selectedItems.Select(x => x.Items.Aggregate((s1, s2) => $"{s1}, {s2}")).Aggregate((s1, s2) => $"{s1}{Environment.NewLine}> {s2}")}"
                : $"已經取消所有通知! 你不會再收到提醒囉!";
            await component.UpdateAsync(x =>
            {
                x.Content = text;
                x.Components = null;
            });
        }
    }
}
