using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Db.Entity;
using DocumentFormat.OpenXml.Drawing;

namespace DiscordBot.SemanticKernel.Core
{
    public class KernelStatus()
    {
        public Queue<StepStatus> StepStatuses { get; set; } = new();
        public Conversation Conversation { get; set; } = new();
    }

    public class StepStatus
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public StatusEnum Status { get; set; }
        public string Message { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan? ElapsedTime
        {
            get
            {
                if (!ShowElapsedTime) return null;

                DateTime _endTime = EndTime == default ? DateTime.Now : EndTime;
                TimeSpan elapsedTime = _endTime - StartTime;
                return elapsedTime.TotalSeconds == 0 ? null : _endTime - StartTime;
            }
        }

        public bool ShowElapsedTime { get; set; } = false;
    }

    public enum StatusEnum
    {
        Pending,
        Thinking,
        Running,
        Completed,
        Failed
    }
}
