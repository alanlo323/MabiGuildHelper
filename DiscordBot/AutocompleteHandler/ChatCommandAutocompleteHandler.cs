using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.DataObject;
using DiscordBot.Helper;
using DiscordBot.Util;

namespace DiscordBot.ButtonHandler
{
    public class ChatCommandAutocompleteHandler(EnchantmentHelper enchantmentHelper, ItemHelper itemHelper) : IBaseAutocompleteHandler
    {
        public const string PrefixMabi = "瑪奇";
        public const string PrefixEnchantment = "魔力賦予";
        public const string PrefixItem = "物品";

        public string CommandName { get; set; } = "chat";

        public async Task Excute(SocketAutocompleteInteraction interaction)
        {
            List<AutocompleteResult> results = [];
            string keyword = interaction.Data.Options.First(x => x.Name == "text").Value as string;
            await CheckEnchantment(results, keyword);
            await CheckItem(results, keyword);
            results = results.Take(25).ToList();
            await interaction.RespondAsync(results);
        }

        private async Task CheckEnchantment(List<AutocompleteResult> results, string keyword)
        {
            if (!keyword.Any(x => PrefixMabi.Any(y => x == y)) && !keyword.Any(x => PrefixEnchantment.Any(y => x == y))) return;

            EnchantmentResponseDto enchantmentResponseDto = await enchantmentHelper.GetEnchantmentsAsync(keyword);
            foreach (Enchantment enchantment in enchantmentResponseDto.Data.Enchantments.Take(25))
            {
                string autocomputeName = $"{PrefixEnchantment} {enchantment.LocalName} / {enchantment.Name}";
                string autocomputeValue = $"{PrefixEnchantment} {enchantment.LocalName}";
                results.Add(new(autocomputeName, autocomputeValue));
            }
        }

        private async Task CheckItem(List<AutocompleteResult> results, string keyword)
        {
            if (!keyword.Any(x => PrefixMabi.Any(y => x == y)) && !keyword.Any(x => PrefixItem.Any(y => x == y))) return;

            ItemSearchResponseDto itemResponseDto = await itemHelper.GetItemsAsync(keyword);
            foreach (Item item in itemResponseDto.Data.Items.Take(25))
            {
                string autocomputeName = $"{PrefixItem} {item.TextName1}";
                string autocomputeValue = $"{PrefixItem} {item.TextName1}";
                results.Add(new(autocomputeName, autocomputeValue));
            }
        }
    }
}
