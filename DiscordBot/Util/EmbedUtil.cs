using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Configuration;
using DiscordBot.DataEntity;
using DiscordBot.Db.Entity;
using DiscordBot.Helper;
using Newtonsoft.Json.Linq;
using Quartz;
using Color = Discord.Color;

namespace DiscordBot.Util
{
    public class EmbedUtil
    {
        public static Embed GetErinnTimeEmbed(bool roundToTenMins = false)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("⏱愛爾琳時間⏱")
                .WithDescription($"{GameUtil.GetErinnTime(roundToTenMins):tt h:mm}")
                .WithFooter("現實時間")
                .WithCurrentTimestamp();
            return embed.Build();
        }

        public static Embed GetDailyEffectEmbed(GameConfig gameConfig)
        {
            DateTime today = DateTime.Now;
            string todayOfWeek = today.DayOfWeek.ToString();
            DailyEffect todayEffect = gameConfig.DailyEffect.First(x => x.DayOfWeek == todayOfWeek);
            DailyBankGift todayBankGift = gameConfig.DailyBankGift.First(x => x.DayOfWeek == todayOfWeek);

            List<EmbedFieldBuilder> embedFieldBuilders = [];

            EmbedFieldBuilder embedFieldEffect = new EmbedFieldBuilder()
                .WithName(today.ToString("yyyy/MM/dd"))
                .WithValue(todayEffect.Effect.Aggregate((s1, s2) => $"{s1}\n{s2}"))
                .WithIsInline(false);
            embedFieldBuilders.Add(embedFieldEffect);

            EmbedFieldBuilder embedFieldBankGift = new EmbedFieldBuilder()
                .WithName("今日銀行禮物")
                .WithValue(todayBankGift.Items.Aggregate((s1, s2) => $"{s1}\n{s2}"))
                .WithIsInline(false);
            embedFieldBuilders.Add(embedFieldBankGift);

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle($"{todayEffect.Title}")
                .WithFields(embedFieldBuilders);
            return embed.Build();
        }

        public static Embed GetTodayDungeonInfoEmbed(DailyDungeonContainer dailyDungeonContainer)
        {
            List<EmbedFieldBuilder> embedFieldBuilders = [];
            var cultureInfo = new CultureInfo("zh-tw");
            var dateTimeInfo = cultureInfo.DateTimeFormat;
            var dungeonInfoList = dailyDungeonContainer.Infos;

            DateTime today = DateTime.Now.Date;
            var todayDungeonInfo = dungeonInfoList
                .Where(x => x.IsTodayDungeon)
                .First()
                ;

            EmbedFieldBuilder embedField = new EmbedFieldBuilder()
                .WithName($"{todayDungeonInfo.Date:yyyy-MM-dd}")
                .WithValue(todayDungeonInfo.Name)
                ;
            embedFieldBuilders.Add(embedField);

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Blue)
                .WithTitle("今日老手地下城")
                .WithFields(embedFieldBuilders)
                .WithFooter("更新時間")
                .WithCurrentTimestamp()
                .WithImageUrl($"attachment://{dailyDungeonContainer.GetImageTempFile().Name}")
                ;

            return embed.Build();
        }

        public static Embed GetResetReminderEmbed(string description, Color color, IEnumerable<InstanceReset> instanceResets, bool useNextDateTime = true)
        {
            List<EmbedFieldBuilder> embedFieldBuilders = [];

            foreach (var item in instanceResets)
            {
                EmbedFieldBuilder embedField = new EmbedFieldBuilder()
                    .WithName(item.Name)
                    .WithIsInline(true)
                    .WithValue($"<t:{DateTimeUtil.ConvertToTimestamp(useNextDateTime ? item.NextResetDateTime : item.LastResetDateTime)}:R>")
                    ;
                embedFieldBuilders.Add(embedField);
            }

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(color)
                .WithTitle("重置時間表")
                .WithDescription(description)
                .WithFields(embedFieldBuilders)
                ;

            return embed.Build();
        }

        public static Embed GetMainogiNewsEmbed(News news)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle(news.Title)
                .WithDescription(news.Content)
                .WithFooter("更新時間")
                .WithUrl($"{DataScrapingHelper.MabinogiBaseUrl}/{news.Url}")
                .WithTimestamp((DateTimeOffset)news.UpdatedAt)
                .WithImageUrl($"attachment://{news.GetSnapshotTempFile().Name}")
                ;

            return embed.Build();
        }
    }
}
