using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Configuration;
using DiscordBot.DataEntity;
using DiscordBot.Extension;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Util
{
    public static class GameUtil
    {
        private static readonly string DungeonHtmlStylePath = $"AppData/DailyDungeonsHtmlStyle.html";

        public static DateTime GetErinnTime(bool roundToTenMins = false)
        {
            var offset = -10;
            var now = DateTime.Now;
            var erinnTimeSecond = (now.Hour * 60 * 60) + (now.Minute * 60) + now.Second;
            var errinDateTime = DateTime.MinValue.AddSeconds(erinnTimeSecond * 40).AddMinutes(offset);
            if (roundToTenMins) errinDateTime = errinDateTime.AddMinutes(-(errinDateTime.Minute % 10));
            return errinDateTime;
        }

        public async static Task<DailyDungeonContainer> GetDailyDungeons()
        {
            List<DailyDungeonInfo> dungeonInfoList = new();
            DateTime baseDate = new(2023, 10, 15);
            DateTime today = DateTime.Today;
            DateTime now = DateTime.Now;

            int pos = (int)today.DayOfWeek + 7;
            if (pos == 7)
            {
                pos = 14;
            }
            DateTime day = today.AddDays(pos * (-1));
            int vetSize = DailyDungeonInfo.veteran.Length;
            int vet = (int)((day - baseDate).TotalDays) % vetSize;

            string html = string.Empty;
            string style = File.ReadAllText(DungeonHtmlStylePath);
            html += style;
            html += Environment.NewLine;
            html += "<table class=\"out\" id=\"dungeons\">";
            html += Environment.NewLine;
            html += "<thead><tr>";
            html += Environment.NewLine;
            var cultureInfo = new CultureInfo("zh-tw");
            var dateTimeInfo = cultureInfo.DateTimeFormat;
            foreach (DayOfWeek dayOfWeek in Enum.GetValues(typeof(DayOfWeek)))
            {
                string dayOfWeekStr = dateTimeInfo.GetDayName(dayOfWeek);
                html += $"<th>{dayOfWeekStr}</th>";
                html += Environment.NewLine;
            }
            html += "</tr></thead>";
            html += Environment.NewLine;
            for (int i = 0; i < 28; i++)
            {
                var dungeonInfo = new DailyDungeonInfo()
                {
                    Name = DailyDungeonInfo.veteran[vet % vetSize],
                    Date = day.Date,
                };
                dungeonInfoList.Add(dungeonInfo);

                int dayOfWeek = i % 7;
                if (dayOfWeek == 0)
                {
                    html += "<tr>";
                    html += Environment.NewLine;
                }
                html += $"<td{(dungeonInfo.IsTodayDungeon ? " class='today'" : dayOfWeek == 0 || dayOfWeek == 6 ? " class='holiday'" : string.Empty)}>{Environment.NewLine}<div class='date'>{day:MM-dd}</div>{Environment.NewLine}<table class='in'><tr><td>{DailyDungeonInfo.veteran[vet % vetSize]}</td></tr></table>{Environment.NewLine}</td>";
                html += Environment.NewLine;
                day = day.AddDays(1);
                vet++;
                if (dayOfWeek == 6)
                {
                    html += $"</tr>";
                    html += Environment.NewLine;
                }
            }
            html += "</table>";
            html += Environment.NewLine;

            DailyDungeonContainer dailyDungeonContainer = new()
            {
                Infos = dungeonInfoList,
                Html = html,
            };

            return dailyDungeonContainer;
        }
    }
}
