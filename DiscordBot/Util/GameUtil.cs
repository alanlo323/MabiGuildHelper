using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Extension;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Util
{
    public static class GameUtil
    {
        public static TimeSpan GetErinnTime(bool roundToTenMins = false)
        {
            var offset = -9;
            var now = DateTime.Now;
            var erinnTimeSecond = (now.Hour * 60 * 60) + (now.Minute * 60) + now.Second;
            var errinDateTime = DateTime.MinValue.AddSeconds(erinnTimeSecond * 40).AddMinutes(offset);
            if (roundToTenMins) errinDateTime = errinDateTime.AddMinutes(-(errinDateTime.Minute % 10));
            return errinDateTime.TimeOfDay;
        }
    }
}
