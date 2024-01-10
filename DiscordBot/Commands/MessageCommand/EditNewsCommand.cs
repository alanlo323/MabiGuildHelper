using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.Util;

namespace DiscordBot.Commands.MessageCommand
{
    public class EditNewsCommand(DiscordSocketClient client, AppDbContext appDbContext) : IBaseMessageCommand
    {
        public string Name { get; set; } = "編輯官方通告";
        public string Description { get; set; }

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new MessageCommandBuilder()
                .WithName(Name)
                .WithDefaultMemberPermissions(GuildPermission.Administrator)
                ;
            return command.Build();
        }

        public async Task Excute(SocketMessageCommand command)
        {
            var message = command.Data.Message;
            if (message.Author.Id != client.CurrentUser.Id || message.Embeds.Count != 1)
            {
                await command.RespondAsync("此指令只能對小幫手轉載的官方通告使用", ephemeral: true);
                return;
            }

            Modal modal = ModalUtil.GetEditNewsModal(message, Name);
            await command.RespondWithModalAsync(modal);
        }
    }
}
