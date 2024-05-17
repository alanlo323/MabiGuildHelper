using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Db.Entity;

namespace DiscordBot.SemanticKernel.Core
{
    public class KernelStatus()
    {
        public Queue<StepStatus> StepStatuses { get; set; } = new();
        public Conversation Conversation { get; set; } = new();
    }
    
    public class StepStatus
    {
        public string Name { get; set; }
        public StatusEnum Status { get; set; }
        public string Message { get; set; }
    }

    public enum StatusEnum
    {
        Pending,
        Running,
        Completed,
        Failed
    }
}
