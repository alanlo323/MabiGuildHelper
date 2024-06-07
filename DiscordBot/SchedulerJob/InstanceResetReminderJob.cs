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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.SchedulerJob
{
    public class InstanceResetReminderJob(ILogger<InstanceResetReminderJob> logger, DiscordSocketClient client, AppDbContext appDbContext, IOptionsSnapshot<GameConfig> gameConfig, DiscordApiHelper discordApiHelper, ButtonHandlerHelper buttonHandlerHelper) : IJob
    {
        public static readonly JobKey Key = new(nameof(InstanceResetReminderJob));
        GameConfig _gameConfig = gameConfig.Value;

        public async Task Execute(IJobExecutionContext context)
        {
            await RefreshTimeTable();
            await SendNotification();
        }

        public async Task RefreshTimeTable()
        {
            MessageComponent addReminderButtonComponent = buttonHandlerHelper.GetButtonHandler<ManageReminderButtonHandler>().GetMessageComponent();
            List<InstanceReset> instanceResetList = [.. _gameConfig.InstanceReset.OrderBy(x => x.Type).ThenBy(x => x.Id)];
            Embed resetInOneEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.ResetInOneDay, Color.Red, instanceResetList.Where(x => x.ResetInOneDay));
            Embed battleEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.Battle, Color.Blue, instanceResetList.Where(x => x.Type == InstanceReset.Constant.Battle));
            Embed lifeEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.Life, Color.Blue, instanceResetList.Where(x => x.Type == InstanceReset.Constant.Life));
            Embed miscEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.Misc, Color.Blue, instanceResetList.Where(x => x.Type == InstanceReset.Constant.Misc));
            Embed resetTodayEmbed = EmbedUtil.GetResetReminderEmbed(InstanceReset.Constant.ResetToday, Color.Green, instanceResetList.Where(x => x.ResetToday), useNextDateTime: false);

            var guildSettings = appDbContext.GuildSettings.ToList();

            foreach (GuildSetting guildSetting in guildSettings)
            {
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdBattle), embed: battleEmbed);
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdLife), embed: lifeEmbed);
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdMisc), embed: miscEmbed);
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdToday), embed: resetTodayEmbed);
                await discordApiHelper.UpdateOrCreateMeesage(guildSetting, nameof(GuildSetting.InstanceResetReminderChannelId), nameof(GuildSetting.InstanceResetReminderMessageIdOneDay), messageComponent: addReminderButtonComponent, embed: resetInOneEmbed);
            }
        }

        public async Task SendNotification()
        {
            DateTime now = DateTime.Now;
            Dictionary<int, InstanceReset> instanceResetList = _gameConfig.InstanceReset.ToDictionary(x => x.Id);
            Dictionary<int, DailyVipGift> dailyVipGiftList = _gameConfig.DailyVipGift.ToDictionary(x => x.Id);
            List<GuildUserSetting> guildUserSettings = [.. appDbContext.GuildUserSettings.Include(x => x.InstanceReminderSettings).Include(x => x.DailyVipGiftReminderSettings)];
            foreach (GuildUserSetting guildUserSetting in guildUserSettings)
            {
                await SendInstanceResetReminder(instanceResetList, guildUserSetting, now);
                await SendDailyVipGiftReminder(dailyVipGiftList, guildUserSetting, now);
            }
        }

        private async Task SendInstanceResetReminder(Dictionary<int, InstanceReset> instanceResetList, GuildUserSetting guildUserSetting, DateTime now)
        {
            try
            {
                if (guildUserSetting.InstanceReminderSettings == null || guildUserSetting.InstanceReminderSettings.Count == 0) return;

                var user = await client.GetUserAsync(guildUserSetting.UserId);
                if (user == null) return;

                List<string> reminders = [];

                foreach (InstanceReminderSetting setting in guildUserSetting.InstanceReminderSettings)
                {
                    try
                    {
                        InstanceReset instanceReset = instanceResetList[setting.ReminderId];
                        if (instanceReset.ResetOnDayOfWeek == now.DayOfWeek && instanceReset.ResetOnTime.Hour == now.Hour && instanceReset.ResetOnTime.Minute == now.Minute)
                        {
                            reminders.Add($"`{instanceReset.Name}`");
                        }

                    }
                    catch (Exception e2)
                    {
                        logger.LogError(e2, e2.Message);
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
                logger.LogError(e1, e1.Message);
            }
        }

        private async Task SendDailyVipGiftReminder(Dictionary<int, DailyVipGift> dailyVipGiftList, GuildUserSetting guildUserSetting, DateTime now)
        {
            try
            {
                if (guildUserSetting.DailyVipGiftReminderSettings == null || guildUserSetting.DailyVipGiftReminderSettings.Count == 0) return;

                var user = await client.GetUserAsync(guildUserSetting.UserId);
                if (user == null) return;

                List<string> reminders = [];

                foreach (DailyVipGiftReminderSetting setting in guildUserSetting.DailyVipGiftReminderSettings)
                {
                    try
                    {
                        DailyVipGift dailyVipGift = dailyVipGiftList[setting.ReminderId];
                        if (dailyVipGift.DayOfWeek == now.DayOfWeek && (true || now.Hour == 7 && now.Minute == 0))    // Send on 7:00 a.m.
                        {
                            reminders.Add($"`{dailyVipGift.Items.Aggregate((s1, s2) => $"{s1}, {s2}")}`");
                        }

                    }
                    catch (Exception e2)
                    {
                        logger.LogError(e2, e2.Message);
                    }
                }

                if (reminders.Count > 0)
                {
                    string msg = $"今天的VIP禮物是 {reminders.Aggregate((s1, s2) => $"{s1}、 {s2}")} 記得上線領取喔~";
                    await user.SendMessageAsync(msg);
                }
            }
            catch (Exception e1)
            {
                logger.LogError(e1, e1.Message);
            }
        }
    }
}
