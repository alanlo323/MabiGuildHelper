using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Migrations;
using DiscordBot.Util;
using Irony.Parsing;
using Microsoft.SemanticKernel.ChatCompletion;
using OpenTelemetry.Logs;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace DiscordBot.Db.Entity
{

    public class Conversation : BaseEntity
    {
        public int Id { get; set; }
        public ulong DiscordMessageId { get; set; }
        public string? UserPrompt { get; set; }
        public string? PlanTemplate { get; set; }
        public string? Result { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public string? ChatHistoryJson { get; set; }

        private readonly double promptCost = 0.00015;   //  per 1000 tokens
        private readonly double completionCost = 0.0006;   //  per 1000 tokens

        [NotMapped]
        public double EstimatedCostInUSD { get => (PromptTokens * promptCost / 1000) + (CompletionTokens * completionCost / 1000); }
        [NotMapped]
        //public string DisplayEstimatedCost { get => $"US${EstimatedCostInUSD.ToString("#,##0.#####", CultureInfo.CreateSpecificCulture("zh-us"))}"; }
        public string DisplayEstimatedCost { get => $"{(EstimatedCostInUSD * 7.8).ToString("C4", CultureInfo.CreateSpecificCulture("zh-hk"))}, NT{(EstimatedCostInUSD * 32.3).ToString("C4", CultureInfo.CreateSpecificCulture("zh-tw"))}"; }
        [NotMapped]
        public TimeSpan? ElapsedTime { get => EndTime - StartTime; }
        [NotMapped]
        public ChatHistory? ChatHistory { get; set; }

        public void SetTokens(ICollection<LogRecord> logRecords)
        {
            var tokenLogs = logRecords.Where(x => x.CategoryName == "Microsoft.SemanticKernel.Connectors.OpenAI.AzureOpenAIChatCompletionService").ToList();
            SetTokens(tokenLogs, nameof(PromptTokens));
            SetTokens(tokenLogs, nameof(CompletionTokens));
            SetTokens(tokenLogs, nameof(TotalTokens));
        }

        private void SetTokens(ICollection<LogRecord> tokenLogs, string proprotyName)
        {
            var tokens = tokenLogs.Sum(x => x.Attributes.Where(attr => attr.Key == proprotyName).Sum(attr => int.Parse($"{attr.Value ?? 0}")));
            this.SetProperty(proprotyName, tokens);
        }
    }
}
