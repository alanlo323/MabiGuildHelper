using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Configuration;
using DiscordBot.DataEntity;
using DiscordBot.Helper;
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
                .WithDescription($"{GameUtil.GetErinnTime(roundToTenMins).ToString(@"tt h:mm")}")
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

            List<EmbedFieldBuilder> embedFieldBuilders = new();

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

        public static Embed GetTodayDungeonInfoEmbed(ImgurHelper imgurHelper)
        {
            return GetTodayDungeonInfoEmbed(imgurHelper, out DailyDungeonInfo todayDungeonInfo);
        }

        public static Embed GetTodayDungeonInfoEmbed(ImgurHelper imgurHelper, out DailyDungeonInfo ouputTodayDungeonInfo)
        {
            List<EmbedFieldBuilder> embedFieldBuilders = new();
            var cultureInfo = new CultureInfo("zh-tw");
            var dateTimeInfo = cultureInfo.DateTimeFormat;
            var dungeonInfoContain = GameUtil.GetDailyDungeons().Result;
            var dungeonInfoList = dungeonInfoContain.Infos;
            var imageUrl = imgurHelper.UploadImage(dungeonInfoContain.Image).Result;

            DateTime today = DateTime.Now.Date;
            var todayDungeonInfo = dungeonInfoList
                .Where(x => x.IsTodayDungeon)
                .First()
                ;
            ouputTodayDungeonInfo = todayDungeonInfo;

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
                ;
            if (!string.IsNullOrEmpty(imageUrl)) embed = embed.WithImageUrl(imageUrl);

            return embed.Build();
        }
    }
}
