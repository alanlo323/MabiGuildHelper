﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SelectMenuHandler;
using DiscordBot.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.MessageHandler
{
    public partial class MessageReceivedHandler(ILogger<MessageReceivedHandler> logger, DiscordSocketClient client, IOptionsSnapshot<FunnyResponseConfig> funnyResponseConfig, AppDbContext appDbContext)
    {
        FunnyResponseConfig _funnyResponseConfig = funnyResponseConfig.Value;

        public async Task Excute(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message) return;
            if (message.Author.Id == client.CurrentUser.Id) return;

            await CheckFunnyResponse(message);
        }

        private async Task CheckFunnyResponse(SocketUserMessage message)
        {
            await CheckCromBasTrigger(message);
            await CheckWordTrigger(message);
            await CheckStickerTrigger(message);
            await CheckCountOffTrigger(message);
        }

        private async Task CheckWordTrigger(SocketUserMessage message)
        {
            var triggeredWords = _funnyResponseConfig.TriggerWords.Where(x => message.Content.ToLower().Contains(x.ToLower()));
            if (triggeredWords.Any())
            {
                string triggeredWord = triggeredWords.First();
                string key = $"FunnyResponse:text:{message.Channel.Id}:{triggeredWord}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(1) > DateTime.Now) return;   //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = DateTime.Now;

                bool replyMessage = false;
                AppDataHelper appDataHelper = new();
                FileAttachment[] fileAttachments = new[] { new FileAttachment(appDataHelper.GetFunnyResponseFile().FullName) };

                await message.Channel.SendFilesAsync(fileAttachments, messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyResponse|${key}");
            }
        }

        private async Task CheckStickerTrigger(SocketUserMessage message)
        {
            var triggeredStickers = _funnyResponseConfig.TriggerStickers.Where(x => message.Stickers.Any(s => s.Name == x.ToLower()));
            if (triggeredStickers.Any())
            {
                string triggeredSticker = triggeredStickers.First();
                string key = $"FunnyResponse:sticker:{message.Channel.Id}:{triggeredSticker}";
                Tuple<DateTime, int> data = (Tuple<DateTime, int>)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, Tuple.Create(DateTime.MinValue, 0));
                DateTime lastTriggerTime = data.Item1;
                int count = data.Item2;
                if (count == 0 && lastTriggerTime.AddMinutes(30) > DateTime.Now) return;    //  cooldown
                if (lastTriggerTime.AddMinutes(5) < DateTime.Now) count = 0;    //  effective time
                RuntimeDbUtil.DefaultRuntimeDb[key] = Tuple.Create(DateTime.Now, ++count);
                if (count < 2) return;
                RuntimeDbUtil.DefaultRuntimeDb[key] = Tuple.Create(DateTime.Now, 0);

                bool replyMessage = false;
                ISticker[] stickers = message.Stickers.Where(x => x.Name == triggeredSticker).ToArray();

                await message.Channel.SendMessageAsync(stickers: stickers, messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyResponse|{key}");
            }
        }

        private async Task CheckCountOffTrigger(SocketUserMessage message)
        {
            if (message.Content == "1")
            {
                string key = $"FunnyResponse:CountOff:1:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = DateTime.Now;

                bool replyMessage = true;
                await message.Channel.SendMessageAsync(text: "2", messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyResponse|{key}");
            }

            if (message.Content == "3")
            {
                string lastKey = $"FunnyResponse:CountOff:1:{message.Channel.Id}";
                DateTime lastKeyLastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(lastKey, DateTime.MinValue);
                if (lastKeyLastTriggerTime.AddMinutes(30) < DateTime.Now) return;   //  effective time
                string key = $"FunnyResponse:CountOff:3:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = lastKeyLastTriggerTime;

                bool replyMessage = true;
                await message.Channel.SendMessageAsync(text: "4", messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyResponse|{key}");
            }

            if (message.Content == "5")
            {
                string lastKey = $"FunnyResponse:CountOff:3:{message.Channel.Id}";
                DateTime lastKeyLastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(lastKey, DateTime.MinValue);
                if (lastKeyLastTriggerTime.AddMinutes(30) < DateTime.Now) return;   //  effective time
                string key = $"FunnyResponse:CountOff:5:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = lastKeyLastTriggerTime;

                bool replyMessage = true;
                await message.Channel.SendMessageAsync(text: "6", messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyResponse|{key}");
            }

            if (message.Content == "7")
            {
                string lastKey = $"FunnyResponse:CountOff:5:{message.Channel.Id}";
                DateTime lastKeyLastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(lastKey, DateTime.MinValue);
                if (lastKeyLastTriggerTime.AddMinutes(30) < DateTime.Now) return;   //  effective time
                string key = $"FunnyResponse:CountOff:7:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = lastKeyLastTriggerTime;

                bool replyMessage = true;
                AppDataHelper appDataHelper = new();
                FileAttachment[] fileAttachments = [new FileAttachment(appDataHelper.GetFunnyResponseFile().FullName)];

                await message.Channel.SendFilesAsync(fileAttachments, messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyResponse|{key}");
            }
        }

        private async Task CheckCromBasTrigger(SocketUserMessage message)
        {
            if (await appDbContext.GuildSettings.AnyAsync(x => x.CromBasHelperChannelId == message.Channel.Id))
            {
                Regex regex = CromBasHintRegex();
                var match = regex.Match(message.Content);
                if (match.Success)
                {
                    Queue<int> number = new();
                    Queue<string> symbol = new();
                    string postfix = string.Empty;
                    foreach (Group group in match.Groups.Cast<Group>())
                    {
                        switch (group.Name)
                        {
                            case "1":
                                for (int i = 0; i < group.Captures.Count; i++)
                                {
                                    number.Enqueue(group.Captures[i].Value.ToInt());
                                }
                                break;
                            case "2":
                                for (int i = 0; i < group.Captures.Count; i++)
                                {
                                    symbol.Enqueue(group.Captures[i].Value);
                                }
                                break;
                            case "3":
                                postfix = group.Value;
                                break;
                        }
                    }

                    string expression = string.Empty;
                    int numberCount = number.Count;
                    while (number.Count > 0)
                    {
                        expression += $"{number.Dequeue()} ";
                        if (symbol.Count > 0) expression += $"{symbol.Dequeue()} ";
                    }
                    expression = expression.Trim();
                    var numberResult = new DataTable().Compute(expression, string.Empty);
                    string[] posFixes = ["山夏", "立春", "入夏", "秋收", "巴斯"];
                    string result = string.Empty;
                    string mappedPostFix = posFixes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(postfix) && x.StartsWith(postfix));
                    if (string.IsNullOrEmpty(mappedPostFix))
                    {
                        foreach (var item in posFixes)
                        {
                            result += $"{numberResult}{item}{Environment.NewLine}";
                        }
                    }
                    else
                    {
                        result = $"{numberResult}{mappedPostFix}";
                    }
                    result = result.Trim().ToQuotation();

                    await message.ReplyAsync(result);
                }
            }
        }

        [GeneratedRegex(@"(\d+ ){3}.*([\+\-\*\/]){2} *(..?)?", RegexOptions.IgnoreCase)]
        private static partial Regex CromBasHintRegex();
    }
}