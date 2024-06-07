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
    public class AddInstanceResetReminderSelectMenuHandler(ILogger<AddInstanceResetReminderSelectMenuHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, IOptionsSnapshot<GameConfig> gameConfig) : IBaseSelectMenuHandler
    {
        GameConfig _gameConfig = gameConfig.Value;

        public string Id { get; set; } = "AddInstanceResetReminderSelectMenu";

        public MessageComponent GetMessageComponent(IEnumerable<IReminderSetting> reminderSettings)
        {
            List<InstanceReset> instanceResetList = [.. _gameConfig.InstanceReset.OrderBy(x => x.Type).ThenBy(x => x.Id)];
            if (instanceResetList.Count == 0) throw new NotSupportedException("Empty list in InstanceReset, please check config.");

            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("按我選擇提醒")
                .WithCustomId(Id)
                ;

            foreach (var instance in instanceResetList)
            {
                menuBuilder.AddOption($"{instance.Type} - {instance.Name}", $"{instance.Id}", description: $"下次重置日期: {instance.NextResetDateTime:yyyy-MM-dd (ddd) tt hh:mm}", isDefault: (reminderSettings as IEnumerable<InstanceReminderSetting>)?.Any(x => x.ReminderId == instance.Id));
            }
            menuBuilder
                .WithMinValues(0)
                .WithMaxValues(instanceResetList.Count)
                ;

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            return builder.Build();
        }

        public async Task Excute(SocketMessageComponent component)
        {
            List<InstanceReset> selectedItems = _gameConfig.InstanceReset.Where(x => component.Data.Values.Contains($"{x.Id}")).ToList();
            IEnumerable<InstanceReminderSetting> existingSettings = [.. appDbContext.InstanceReminderSettings.Where(x => x.GuildId == component.GuildId && x.UserId == component.User.Id)];
            IEnumerable<InstanceReminderSetting> nonSelectedSettings = existingSettings.Where(x => !selectedItems.Any(y => y.Id == x.ReminderId)).ToList();

            foreach (var item in selectedItems)
            {
                await databaseHelper.GetOrCreateEntityByKeys<InstanceReminderSetting>(
                    new() {
                        { nameof(InstanceReminderSetting.GuildId), component.GuildId },
                        { nameof(InstanceReminderSetting.UserId), component.User.Id },
                        { nameof(InstanceReminderSetting.ReminderId), item.Id },
                    });
            }

            appDbContext.RemoveRange(nonSelectedSettings);
            await appDbContext.SaveChangesAsync();

            string text = selectedItems.Count > 0
                ? $"設定已更新, 小幫手會在下方的事件重置時通知你喔! {Environment.NewLine}> {selectedItems.Select(x => x.Name).Aggregate((s1, s2) => $"{s1}{Environment.NewLine}> {s2}")}"
                : $"已經取消所有通知! 你不會再收到重置提醒囉!";
            await component.UpdateAsync(x =>
            {
                x.Content = text;
                x.Components = null;
            });
        }
    }
}
