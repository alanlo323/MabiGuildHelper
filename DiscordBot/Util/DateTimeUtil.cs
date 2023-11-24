using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Util
{
    public class DateTimeUtil
    {
        private static readonly DateTime Epoch = new(1970, 1, 1, 0, 0, 0);

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

        public static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            elapsedTime = elapsedTime.Add(TimeSpan.FromHours(8) * -1);
            return (long)elapsedTime.TotalSeconds;
        }
    }
}
