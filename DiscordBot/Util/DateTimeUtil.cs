using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Util
{
    public class DateTimeUtil
    {
        public static DateTime GetNextGivenTime(int hour, int minute, int second)
        {
            DateTime dt = DateTime.Today
                .AddHours(hour)
                .AddMinutes(minute)
                .AddSeconds(second);
            if (dt < DateTime.Now)
                dt = dt.AddDays(1);
            return dt;
        }
    }
}
