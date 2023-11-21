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
    public class AddReminderSelectMenuHandler : IBaseSelectMenuHandler
    {
        ILogger<AddReminderSelectMenuHandler> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        IServiceProvider _serviceProvider;
        DatabaseHelper _databaseHelper;
        GameConfig _gameConfig;

        public string Id { get; set; } = "AddReminderSelectMenu";

        public AddReminderSelectMenuHandler(ILogger<AddReminderSelectMenuHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, IOptionsSnapshot<GameConfig> gameConfig)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _serviceProvider = serviceProvider;
            _databaseHelper = databaseHelper;
            _gameConfig = gameConfig.Value;
        }

        public MessageComponent GetMessageComponent(IEnumerable<InstanceReminderSetting> InstanceReminderSettings)
        {
            List<InstanceReset> instanceResetList = _gameConfig.InstanceReset.ToList();
            if (instanceResetList.Count == 0) throw new NotSupportedException("Empty list in InstanceReset, please check config.");

            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("按我選擇提醒")
                .WithCustomId(Id)
                ;

            foreach (var instance in instanceResetList)
            {
                menuBuilder.AddOption($"{instance.Type} - {instance.Name}", $"{instance.Id}", description: $"下次重置日期: {instance.NextResetDateTime:yyyy-MM-dd (ddd) tt hh:mm}", isDefault: InstanceReminderSettings?.Any(x => x.InstanceReminderId == instance.Id));
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
            await component.DeferAsync();

            List<InstanceReset> selectedItems = _gameConfig.InstanceReset.Where(x => component.Data.Values.Contains($"{x.Id}")).ToList();
            IEnumerable<InstanceReminderSetting> existingSettings = _appDbContext.InstanceReminderSettings.Where(x => x.GuildId == component.GuildId && x.UserId == component.User.Id).ToList();
            IEnumerable<InstanceReminderSetting> nonSelectedSettings = existingSettings.Where(x => !selectedItems.Any(y => y.Id == x.InstanceReminderId)).ToList();

            foreach (var instance in selectedItems)
            {
                await _databaseHelper.GetOrCreateEntityByKeys<InstanceReminderSetting>(
                    new() {
                        { nameof(InstanceReminderSetting.GuildId), component.GuildId },
                        { nameof(InstanceReminderSetting.UserId), component.User.Id },
                        { nameof(InstanceReminderSetting.InstanceReminderId), instance.Id },
                    });
            }

            _appDbContext.RemoveRange(nonSelectedSettings);
            await _appDbContext.SaveChangesAsync();

            var guildUserSettings = _appDbContext.GuildUserSettings.ToList();

            string text;
            if (selectedItems.Count > 0)
            {
                text = $"設定已更新, 小幫手會在下方的事件重置時通知你喔! {Environment.NewLine}> {selectedItems.Select(x => x.Name).Aggregate((s1, s2) => $"{s1}{Environment.NewLine}> {s2}")}";
            }
            else
            {
                text = $"已經取消所有通知! 你不會再收到重置提醒囉!";
            }
            await component.FollowupAsync(text: text, ephemeral: true);
        }
    }
}
