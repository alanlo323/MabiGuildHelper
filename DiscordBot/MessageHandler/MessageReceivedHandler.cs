using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Helper;
using DiscordBot.SelectMenuHandler;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.MessageHandler
{
    public class MessageReceivedHandler(ILogger<MessageReceivedHandler> logger, DiscordSocketClient client, IOptionsSnapshot<FunnyResponseConfig> funnyResponseConfig)
    {
        FunnyResponseConfig _funnyResponseConfig = funnyResponseConfig.Value;

        public async Task Excute(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message) return;
            if (message.Author.Id == client.CurrentUser.Id) return;

            await CheckFunnyReponse(message);
        }

        private async Task CheckFunnyReponse(SocketUserMessage message)
        {
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
                string key = $"FunnyReponse:text:{message.Channel.Id}:{triggeredWord}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(1) > DateTime.Now) return;   //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = DateTime.Now;

                bool replyMessage = false;
                AppDataHelper appDataHelper = new();
                FileAttachment[] fileAttachments = new[] { new FileAttachment(appDataHelper.GetFunnyResponseFile().FullName) };

                await message.Channel.SendFilesAsync(fileAttachments, messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyReponse|${key}");
            }
        }

        private async Task CheckStickerTrigger(SocketUserMessage message)
        {
            var triggeredStickers = _funnyResponseConfig.TriggerStickers.Where(x => message.Stickers.Any(s => s.Name == x.ToLower()));
            if (triggeredStickers.Any())
            {
                string triggeredSticker = triggeredStickers.First();
                string key = $"FunnyReponse:sticker:{message.Channel.Id}:{triggeredSticker}";
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
                logger.LogInformation($"CheckFunnyReponse|{key}");
            }
        }

        private async Task CheckCountOffTrigger(SocketUserMessage message)
        {
            if (message.Content == "1")
            {
                string key = $"FunnyReponse:countoff:1:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = DateTime.Now;

                bool replyMessage = true;
                await message.Channel.SendMessageAsync(text: "2", messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyReponse|{key}");
            }

            if (message.Content == "3")
            {
                string lastKey = $"FunnyReponse:countoff:1:{message.Channel.Id}";
                DateTime lastKeyLastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(lastKey, DateTime.MinValue);
                if (lastKeyLastTriggerTime.AddMinutes(30) < DateTime.Now) return;   //  effective time
                string key = $"FunnyReponse:countoff:3:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = lastKeyLastTriggerTime;

                bool replyMessage = true;
                await message.Channel.SendMessageAsync(text: "4", messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyReponse|{key}");
            }

            if (message.Content == "5")
            {
                string lastKey = $"FunnyReponse:countoff:3:{message.Channel.Id}";
                DateTime lastKeyLastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(lastKey, DateTime.MinValue);
                if (lastKeyLastTriggerTime.AddMinutes(30) < DateTime.Now) return;   //  effective time
                string key = $"FunnyReponse:countoff:5:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = lastKeyLastTriggerTime;

                bool replyMessage = true;
                await message.Channel.SendMessageAsync(text: "6", messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyReponse|{key}");
            }

            if (message.Content == "7")
            {
                string lastKey = $"FunnyReponse:countoff:5:{message.Channel.Id}";
                DateTime lastKeyLastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(lastKey, DateTime.MinValue);
                if (lastKeyLastTriggerTime.AddMinutes(30) < DateTime.Now) return;   //  effective time
                string key = $"FunnyReponse:countoff:7:{message.Channel.Id}";
                DateTime lastTriggerTime = (DateTime)RuntimeDbUtil.DefaultRuntimeDb.GetValueOrDefault(key, DateTime.MinValue);
                if (lastTriggerTime.AddMinutes(30) > DateTime.Now) return;  //  cooldown
                RuntimeDbUtil.DefaultRuntimeDb[key] = lastKeyLastTriggerTime;

                bool replyMessage = true;
                AppDataHelper appDataHelper = new();
                FileAttachment[] fileAttachments = new[] { new FileAttachment(appDataHelper.GetFunnyResponseFile().FullName) };

                await message.Channel.SendFilesAsync(fileAttachments, messageReference: replyMessage ? new(message.Id) : null);
                logger.LogInformation($"CheckFunnyReponse|{key}");
            }
        }
    }
}