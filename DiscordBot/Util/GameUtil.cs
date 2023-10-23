using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Configuration;
using DiscordBot.Extension;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Util
{
    public static class GameUtil
    {
        public static DateTime GetErinnTime(bool roundToTenMins = false)
        {
            var offset = -10;
            var now = DateTime.Now;
            var erinnTimeSecond = (now.Hour * 60 * 60) + (now.Minute * 60) + now.Second;
            var errinDateTime = DateTime.MinValue.AddSeconds(erinnTimeSecond * 40).AddMinutes(offset);
            if (roundToTenMins) errinDateTime = errinDateTime.AddMinutes(-(errinDateTime.Minute % 10));
            return errinDateTime;
        }

        public static Embed GetErinnTimeEmbed(bool roundToTenMins = false)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("⏱愛爾琳時間⏱")
                .WithDescription($"{GetErinnTime(roundToTenMins).ToString(@"tt h:mm")}")
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
    }
}
