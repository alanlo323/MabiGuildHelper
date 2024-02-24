using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;

namespace DiscordBot.Util
{
    public class ModalUtil
    {

        public static Modal GetEditNewsModal(GuildNewsOverride guildNewsOverride, SocketMessage socketMessage, string title)
        {
            var modalBuilder = new ModalBuilder()
                .WithTitle(title)
                .WithCustomId($"{EditNewsModalHandler.EditNewsModalMasterIdPrefix}_{socketMessage.Id}_{guildNewsOverride.NewsId}")
                .AddTextInput(new TextInputBuilder()
                    .WithLabel("標題")
                    .WithPlaceholder("在這裡輸入通告標題")
                    .WithValue(guildNewsOverride.Title)
                    .WithMinLength(1)
                    .WithMaxLength(50)
                    .WithStyle(TextInputStyle.Short)
                    .WithCustomId(EditNewsModalHandler.EditNewsModalTitleIdPrefix)
                    .WithRequired(true)
                    )
                .AddTextInput(new TextInputBuilder()
                    .WithLabel("內容")
                    .WithPlaceholder("在這裡輸入通告內容")
                    .WithValue(guildNewsOverride.Content)
                    .WithMinLength(0)
                    .WithMaxLength(4000)
                    .WithStyle(TextInputStyle.Paragraph)
                    .WithCustomId(EditNewsModalHandler.EditNewsModalContentIdPrefix)
                    .WithRequired(false)
                    )
                .AddTextInput(new TextInputBuilder()
                    .WithLabel("連結Url")
                    .WithPlaceholder("在這裡輸入要連結的訊息Url")
                    .WithValue(guildNewsOverride.ReleatedMessageUrl)
                    .WithMinLength(0)
                    .WithMaxLength(100)
                    .WithStyle(TextInputStyle.Short)
                    .WithCustomId(EditNewsModalHandler.EditNewsModalReleatedMessageUrlPrefix)
                    .WithRequired(false)
                    )
                ;

            return modalBuilder.Build();
        }
    }
}
