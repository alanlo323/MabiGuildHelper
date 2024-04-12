using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using static DiscordBot.Commands.IBaseCommand;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands.SlashCommand
{
    public class LuckyChannelCommand(ILogger<LuckyChannelCommand> logger, ConcurrentRandomHelper concurrentRandomHelper) : IBaseSlashCommand
    {
        public string Name { get; set; } = "luckychannel";
        public string Description { get; set; } = "獲得今天的幸運頻道";
        public CommandAvailability Availability { get; set; } = CommandAvailability.Global;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .WithDefaultMemberPermissions(GuildPermission.SendMessages)
                .AddOption("subject", ApplicationCommandOptionType.String, "指定一個主題", isRequired: false, minLength: 1)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync();

                Stopwatch stopwatch = new();
                stopwatch.Start();

                string seed, evaluate, advice, subject;
                SocketUser user = command.User;
                subject = command.Data.Options.FirstOrDefault(x => x.Name == "subject")?.Value as string;
                seed = subject == null
                    ? $"{user.Id}-{DateTime.Now:yyyy-MM-dd}"
                    : $"{subject}-{DateTime.Now:yyyy-MM-dd}";
                Random random = seed.GetRandomFromSeed();
                //Random random = new();


                string msg = "";
                for (int index = 0; index < 1; index++)
                {
                    var luckDrawResult = GetLuckDrawResult(random);
                    int[] border = [18, 37, 30, 12, 3]; //  weight
                    for (int i = 1; i < border.Length; i++)
                    {
                        border[i] = border[i - 1] + border[i];
                    }

                    switch (luckDrawResult.Item2)
                    {
                        case var x when x < border[0]:
                            evaluate = "下下";
                            advice = $"今天的運氣不太好, 但不要灰心, 有時候運氣也是需要累積的";
                            break;
                        case var x when x >= border[0] && x < border[1]:
                            evaluate = "中平";
                            advice = $"今天的運氣一般般, 是普通的一天";
                            break;
                        case var x when x >= border[1] && x < border[2]:
                            evaluate = "中吉";
                            advice = $"今天的運氣不錯, 但還有進步的空間";
                            break;
                        case var x when x >= border[2] && x < border[3]:
                            evaluate = "上吉";
                            advice = $"今天的運氣很好, 有機會發生好事";
                            break;
                        case var x when x >= border[3] && x < border[4]:
                            evaluate = "大吉";
                            advice = $"今天的運氣非常好, 很有機會發生好事";
                            break;
                        default:
                            evaluate = "超級大吉";
                            advice = $"今天的運氣超級好, 有機會發生非常好的事";
                            break;
                    }

                    msg += $"{(subject == null ? "你" : string.Empty)}今天的{subject ?? string.Empty}幸運頻道是 {$"Channel {luckDrawResult.Item1}".ToHighLight()}{Environment.NewLine}幸運指數: {luckDrawResult.Item2.ToString().ToHighLight()} {evaluate}{Environment.NewLine}{advice}{Environment.NewLine + Environment.NewLine}";
                };

                stopwatch.Stop();
                //msg = $"計算耗時: {stopwatch.Elapsed.TotalSeconds}s{Environment.NewLine}{msg}";

                await command.FollowupAsync(msg);
            }
            catch (Exception ex)
            {
                await command.FollowupAsync($"小幫手發生錯誤, 請聯絡開發人員 {ex.Message.ToQuotation()}");
            }
        }

        private static (int, int) GetLuckDrawResult(Random? random)
        {
            (int, KeyValuePair<int, int>) trueResult = Simulate(random);

            int numberOfSimulate = 1_000;
            int trimSizePercent = 2;
            List<int> scores = [];
            for (int i = 0; i < numberOfSimulate; i++)
            {
                scores.Add(Simulate(random).Item1);
            }
            List<int> scoresTrimed = scores.OrderBy(x => x).Skip(scores.Count * trimSizePercent / 100).ToList();
            scoresTrimed = scoresTrimed.Take(scoresTrimed.Count - (scores.Count * trimSizePercent / 100)).ToList();

            // Item1 = channel, Item2 = score (0 - 100, > 100 = very lucky)
            return (trueResult.Item2.Key, (int)Math.Round((double)trueResult.Item1 / scoresTrimed.Max() * 100));
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

            // Item1 = score, Item2 = result
            return (luckyResult.Value - (int)((float)numberOfSimulate / max), luckyResult);
        }
    }
}
