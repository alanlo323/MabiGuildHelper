using Discord;
using Discord.WebSocket;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Helper;
using Microsoft.Extensions.Logging;
using static DiscordBot.Commands.IBaseCommand;

namespace DiscordBot.Commands.SlashCommand
{
    public class EnchantmentCommand(ILogger<EnchantmentCommand> logger, DiscordSocketClient client, ItemHelper itemHelper, AppDbContext appDbContext) : IBaseSlashCommand
    {
        public const string GobleName = "魔力賦予";

        public string Name { get; set; } = GobleName;
        public string Description { get; set; } = "和小幫手對話";
        public CommandAvailability Availability { get; set; } = CommandAvailability.AdminServerOnly;

        public ApplicationCommandProperties GetCommandProperties()
        {
            var command = new SlashCommandBuilder()
                .WithName(Name)
                .WithDescription(Description)
                .AddOption("物品", ApplicationCommandOptionType.String, "要賦予的物品", isRequired: true, isAutocomplete: true)
                .AddOption("能力1", ApplicationCommandOptionType.String, "想要的能力", isRequired: false, isAutocomplete: true)
                .AddOption("數值1", ApplicationCommandOptionType.String, "數值", isRequired: true, isAutocomplete: true)
                ;
            return command.Build();
        }

        public async Task Execute(SocketSlashCommand command)
        {
            string item = command.Data.Options.First(x => x.Name == "物品").Value as string;

            ItemSearchResponseDto itemResponseDto = await itemHelper.GetItemsAsync(item, withDetail: true);

            await command.RespondAsync(text: $"{itemResponseDto}");
        }
    }
}