using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.DataEntity
{
    public class DailyDungeonInfo
    {
        public static readonly string[] veteran = { "高爾", "倫達", "貝卡", "艾菲", "賽維爾", "萊比", "瑪斯", "菲歐納", "巴里" };

        public string Name { get; set; }
        public DateTime Date { get; set; }
        public bool IsTodayDungeon { get => DateTime.Now.Hour < 7 ? Date == DateTime.Today.AddDays(-1) : Date == DateTime.Today; }  // 7:00 AM is the reset time
    }
}
