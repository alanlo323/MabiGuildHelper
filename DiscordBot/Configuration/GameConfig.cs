﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordBot.DataEntity;

namespace DiscordBot.Configuration
{
    public class GameConfig
    {
        public const string SectionName = "Game";

        public string DisplayName { get; set; }
        public DailyEffect[] DailyEffect { get; set; }
        public DailyBankGift[] DailyBankGift { get; set; }
        public InstanceReset[] InstanceReset { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(DisplayName) && DailyEffect != null && DailyBankGift != null && InstanceReset != null;
        }
    }

    public class DailyEffect
    {
        public string DayOfWeek { get; set; }
        public string ChannelName { get; set; }
        public string Title { get; set; }
        public string[] Effect { get; set; }
    }

    public class DailyBankGift
    {
        public string DayOfWeek { get; set; }
        public string[] Items { get; set; }
    }

    public class InstanceReset
    {
        public class Constant
        {
            public static string Battle { get; } = "戰鬥";
            public static string Life { get; } = "生活";
            public static string Misc { get; } = "雜項";
            public static string ResetInOneDay { get; } = "一天內重置";
            public static string ResetToday { get; } = "已在今天重置";
        }

        public int Id { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string ResetOn { get; set; }
        public DayOfWeek ResetOnDayOfWeek
        {
            get
            {
                string resetOnDayOfWeekStr = ResetOn.Split(' ')[0];
                DayOfWeek resetOnDayOfWeek = Enum.Parse<DayOfWeek>(resetOnDayOfWeekStr);
                return resetOnDayOfWeek;
            }
        }
        public DateTime ResetOnTime
        {
            get
            {
                DateTime now = DateTime.Now;
                string resetOnTimeStr = ResetOn.Split(' ')[1];
                int resetOnHour = int.Parse(resetOnTimeStr.Split(':')[0]);
                int resetOnMin = int.Parse(resetOnTimeStr.Split(':')[1]);
                DateTime resetOn = now.Date.AddHours(resetOnHour).AddMinutes(resetOnMin);
                return resetOn;
            }
        }
        public DateTime NextResetDateTime
        {
            get
            {
                DateTime resetOn = ResetOnTime;

                while (resetOn.DayOfWeek != ResetOnDayOfWeek)
                {
                    resetOn = resetOn.AddDays(1);
                }
                if (DateTime.Now >= resetOn) resetOn = resetOn.AddDays(7);
                return resetOn;
            }
        }
        public DateTime LastResetDateTime
        {
            get
            {
                return NextResetDateTime.AddDays(-7);
            }
        }
        public bool ResetInOneDay
        {
            get
            {
                return DateTime.Now.AddDays(1) >= NextResetDateTime;
            }
        }
        public bool ResetToday
        {
            get
            {
                return DateTime.Now < LastResetDateTime.AddDays(1);
            }
        }
    }
}
