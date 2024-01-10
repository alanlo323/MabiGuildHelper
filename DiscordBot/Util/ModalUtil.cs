using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.ButtonHandler;
using DiscordBot.Db.Entity;

namespace DiscordBot.Util
{
    public class ModalUtil
    {
        public static readonly string EditNewsModalMasterIdPrefix = EditNewsModalHandler.CustomIdPrefix;
        public static readonly string EditNewsModalTitleIdPrefix = $"{EditNewsModalMasterIdPrefix}_Title";
        public static readonly string EditNewsModalContentIdPrefix = $"{EditNewsModalMasterIdPrefix}_Content";
        public static readonly string EditNewsModalReleatedMessageUrlPrefix = $"{EditNewsModalMasterIdPrefix}_ReleatedMessageUrl";

        public static Modal GetEditNewsModal(SocketMessage message, string title)
        {
            Embed embed = message.Embeds.Single();

            var modalBuilder = new ModalBuilder()
                .WithTitle(title)
                .WithCustomId($"{EditNewsModalMasterIdPrefix}_{message.Id}")
                .AddTextInput(new TextInputBuilder()
                    .WithLabel("標題")
                    .WithPlaceholder("在這裡輸入通告標題")
                    .WithValue(embed.Title)
                    .WithMinLength(1)
                    .WithMaxLength(30)
                    .WithStyle(TextInputStyle.Short)
                    .WithCustomId($"{EditNewsModalTitleIdPrefix}_{message.Id}")
                    .WithRequired(true)
                    )
                .AddTextInput(new TextInputBuilder()
                    .WithLabel("內容")
                    .WithPlaceholder("在這裡輸入通告內容")
                    .WithValue(embed.Description)
                    .WithMinLength(1)
                    .WithMaxLength(4000)
                    .WithStyle(TextInputStyle.Paragraph)
                    .WithCustomId($"{EditNewsModalContentIdPrefix}_{message.Id}")
                    .WithRequired(true)
                    )
                .AddTextInput(new TextInputBuilder()
                    .WithLabel("連結Url")
                    .WithPlaceholder("在這裡輸入要連結的訊息Url")
                    .WithMinLength(1)
                    .WithMaxLength(100)
                    .WithStyle(TextInputStyle.Short)
                    .WithCustomId($"{EditNewsModalReleatedMessageUrlPrefix}_{message.Id}")
                    .WithRequired(false)
                    )
                ;

            return modalBuilder.Build();
        }
    }
}
