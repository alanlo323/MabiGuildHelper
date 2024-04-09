using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class LuckyChannelCommand(ILogger<LuckyChannelCommand> logger, AppDbContext appDbContext, DiscordApiHelper discordApiHelper, IOptionsSnapshot<DiscordBotConfig> discordBotConfig) : IBaseSlashCommand
    {
        public string Name { get; set; } = "luckychannel";
        public string Description { get; set; } = "今天的幸運頻道";
        public CommandAvailability Availability { get; set; } = CommandAvailability.Global;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.SendMessages)
                .AddOption("subject", ApplicationCommandOptionType.String, "指定另一個主題", isRequired: false, minLength: 1)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            await command.DeferAsync();

            //Thread.Sleep(1000);

            string seed, evaluate, advice;
            SocketUser user = command.User;
            seed = command.Data.Options.FirstOrDefault(x => x.Name == "subject")?.Value is string subject
                ? $"{user.Id}-{subject}-{DateTime.Now:yyyy-MM-dd}"
                : $"{user.Id}-{DateTime.Now:yyyy-MM-dd}";
            Random random = seed.GetRandomFromSeed(); ;
            //Random random = null;

            var luckDrawResult = GetLuckDrawResult(random);
            int[] border = [68, 105, 135, 147, 150];


            switch (luckDrawResult.Item2)
            {
                case var x when (x <= border[0]):
                    evaluate = "下下";
                    advice = "你今天的運氣不太好, 但不要灰心, 有時候運氣也是需要累積的";
                    break;
                case var x when (x > border[0] && x <= border[1]):
                    evaluate = "中平";
                    advice = "你今天的運氣還可以, 但還有進步的空間";
                    break;
                case var x when (x > border[1] && x <= border[2]):
                    evaluate = "中吉";
                    advice = "你今天的運氣不錯, 有機會發生好事";
                    break;
                case var x when (x > border[2] && x <= border[3]):
                    evaluate = "上吉";
                    advice = "你今天的運氣很好, 有機會發生好事";
                    break;
                default:
                    evaluate = "大吉";
                    advice = "你今天的運氣超級好, 有機會發生大好事";
                    break;
            }

            await command.FollowupAsync($"經小幫手計算後, {(subject == null ? "" : "")}你今天的幸運頻道是 {$"Channel {luckDrawResult.Item1}".ToHighLight()}{Environment.NewLine}幸運指數: {luckDrawResult.Item2.ToString().ToHighLight()} {evaluate}{Environment.NewLine}{advice}");
        }

        private static (int, int) GetLuckDrawResult(Random random)
        {
            int numberOfSimulate = 1_000;

            Dictionary<int, int> resultPool = [];

            for (int i = 0; i < numberOfSimulate; i++)
            {
                int randomResult = Simulate(random).Item1;
                resultPool[randomResult] = resultPool.GetValueOrDefault(randomResult) + 1;
            }
            double total = 0;
            foreach (var item in resultPool.OrderBy(x => x.Key))
            {
                total += item.Key * item.Value;
            }
            double avg = total / numberOfSimulate;
            (int, KeyValuePair<int, int>) trueResult = Simulate(random);

            return (trueResult.Item2.Key, (int)Math.Truncate(trueResult.Item1 / avg * 100));
        }

        private static (int, KeyValuePair<int, int>) Simulate(Random? random = null)
        {
            random ??= new();

            int min = 1;
            int max = 13;
            int numberOfSimulate = 100_000;

            Dictionary<int, int> resultPool = [];

            for (int i = 0; i < numberOfSimulate; i++)
            {
                int randomResult = random.Next(min, max + 1);
                resultPool[randomResult] = resultPool.GetValueOrDefault(randomResult) + 1;
            }
            KeyValuePair<int, int> luckyResult = resultPool.MaxBy(x => x.Value);
            return (luckyResult.Value - (int)((float)numberOfSimulate / max), luckyResult);
        }
    }
}
