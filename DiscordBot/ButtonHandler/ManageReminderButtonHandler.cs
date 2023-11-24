using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Commands;
using DiscordBot.Configuration;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SelectMenuHandler;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.ButtonHandler
{
    public class ManageReminderButtonHandler : IBaseButtonHandler
    {
        ILogger<ManageReminderButtonHandler> _logger;
        DiscordSocketClient _client;
        AppDbContext _appDbContext;
        IServiceProvider _serviceProvider;
        DatabaseHelper _databaseHelper;
        SelectMenuHandlerHelper _selectMenuHandlerHelper;

        public string Lable { get; set; } = "管理重置提醒";
        public string Id { get; set; } = "ManageReminderButton";

        public ManageReminderButtonHandler(ILogger<ManageReminderButtonHandler> logger, DiscordSocketClient client, AppDbContext appDbContext, IServiceProvider serviceProvider, DatabaseHelper databaseHelper, SelectMenuHandlerHelper selectMenuHandlerHelper)
        {
            _logger = logger;
            _client = client;
            _appDbContext = appDbContext;
            _serviceProvider = serviceProvider;
            _databaseHelper = databaseHelper;
            _selectMenuHandlerHelper = selectMenuHandlerHelper;
        }

        public MessageComponent GetMessageComponent()
        {
            ComponentBuilder componentBuilder = new ComponentBuilder()
                .WithButton(label: Lable, emote: new Emoji("⚙️"), style: ButtonStyle.Primary, customId: Id);
            return componentBuilder.Build();
        }

        public async Task Excute(SocketMessageComponent component)
        {
            var userSetting = await _databaseHelper.GetOrCreateEntityByKeys<GuildUserSetting>(new() { { nameof(GuildUserSetting.GuildId), component.GuildId }, { nameof(GuildUserSetting.UserId), component.User.Id } });
            MessageComponent messageComponent = _selectMenuHandlerHelper.GetSelectMenuHandler<AddReminderSelectMenuHandler>().GetMessageComponent(userSetting.InstanceReminderSettings);
            await component.RespondAsync(text: "請選擇想要接收的提醒", ephemeral: true, components: messageComponent);
        }
    }
}
