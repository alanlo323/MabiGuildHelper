using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Extension;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Commands
{
    public class AboutCommand : IBaseCommand
    {
        public string Name { get; set; } = "about";
        public string Description { get; set; } = "關於此機器人";

        public SlashCommandProperties GetSlashCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                ;
            return command.Build();
        }

        public async Task Excute(SocketSlashCommand command)
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.LightOrange)
                .WithTitle($"瑪奇公會 夏夜月涼 專屬機器人")
                .WithDescription("此機器人提供與夏夜月涼Discord伺服器使用\n如有想添加功能或錯誤回報, 請聯絡作者@alanlo")
                .WithFooter("Owner @alanlo")
                .WithImageUrl("https://i.imgur.com/2b0utzb.png")
                ;

            await command.RespondAsync(embed: embed.Build());
        }
    }
}
