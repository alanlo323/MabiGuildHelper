using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
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
    public class InstanceResetReminderJob : IJob
    {
        public static readonly JobKey Key = new(nameof(InstanceResetReminderJob));

        ILogger<InstanceResetReminderJob> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        GameConfig _gameConfig;
        DiscordApiHelper _discordApiHelper;
        ButtonHandlerHelper _buttonHandlerHelper;

        public InstanceResetReminderJob(ILogger<InstanceResetReminderJob> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig, DiscordApiHelper discordApiHelper, ButtonHandlerHelper buttonHandlerHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _gameConfig = gameConfig.Value;
            _discordApiHelper = discordApiHelper;
            _buttonHandlerHelper = buttonHandlerHelper;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await RefreshTimeTable();
            await SendNotification();
        }

        public async Task RefreshTimeTable()
        {
            MessageComponent addReminderButtonComponent = _buttonHandlerHelper.GetButtonHandler<ManageReminderButtonHandler>().GetMessageComponent();
            List<InstanceReset> instanceResetList = _gameConfig.InstanceReset.ToList();
            Embed resetInOneEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.ResetInOneDay, Color.Red, instanceResetList.Where(x => x.ResetInOneDay));
            Embed battleEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.Battle, Color.Blue, instanceResetList.Where(x => x.Type == InstanceReset.Constant.Battle));
            Embed lifeEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.Life, Color.Blue, instanceResetList.Where(x => x.Type == InstanceReset.Constant.Life));
            Embed miscEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.Misc, Color.Blue, instanceResetList.Where(x => x.Type == InstanceReset.Constant.Misc));
            Embed resetTodayEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.ResetToday, Color.Green, instanceResetList.Where(x => x.ResetToday), useNextDateTime: false);

            var guildSettings = _appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdBattle), embed: battleEmbed);
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdLife), embed: lifeEmbed);
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdMisc), embed: miscEmbed);
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdToday), embed: resetTodayEmbed);
                await _discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdOneDay), messageComponent: addReminderButtonComponent, embed: resetInOneEmbed);
            }
        }

        public async Task SendNotification()
        {
            DateTime now = DateTime.Now;
            var instanceResetList = _gameConfig.InstanceReset.ToDictionary(x => x.Id);
            var guildUserSettings = _appDbContext.GuildUserSettings.ToList();
            var temp = _appDbContext.InstanceReminderSettings.ToList();
            foreach (GuildUserSetting guildUserSetting in guildUserSettings)
            {
                try
                {
                    if (guildUserSetting.InstanceReminderSettings == null || guildUserSetting.InstanceReminderSettings.Count == 0) continue;

                    var user = await _client.GetUserAsync(guildUserSetting.UserId);
                    if (user == null) continue;

                    List<string> reminders = new();

                    foreach (InstanceReminderSetting instanceReminderSetting in guildUserSetting.InstanceReminderSettings)
                    {
                        try
                        {
                            InstanceReset instanceReset = instanceResetList[instanceReminderSetting.InstanceReminderId];
                            if (instanceReset.ResetOnDayOfWeek == now.DayOfWeek && instanceReset.ResetOnTime.Hour == now.Hour && instanceReset.ResetOnTime.Minute == now.Minute)
                            {
                                reminders.Add($"`{instanceReset.Name}`");
                            }

                        }
                        catch (Exception e2)
                        {
                            _logger.LogError(e2, e2.Message);
                        }
                    }

                    if (reminders.Count > 0)
                    {
                        string msg = $"小幫手提提您, {reminders.Aggregate((s1, s2) => $"{s1}、 {s2}")} 的每周限額已經重置囉~ 賺錢打寶的快去快去~";
                        await user.SendMessageAsync(msg);
                    }
                }
                catch (Exception e1)
                {
                    _logger.LogError(e1, e1.Message);
                }
            }
        }
    }
}
