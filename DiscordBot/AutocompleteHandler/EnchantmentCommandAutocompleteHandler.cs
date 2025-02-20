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
    public class EnchantmentCommandAutocompleteHandler(EnchantmentHelper enchantmentHelper, ItemHelper itemHelper) : IBaseAutocompleteHandler
    {
        public const string PrefixMabi = "瑪奇";
        public const string PrefixEnchantment = "魔力賦予";
        public const string PrefixItem = "物品";

        public string CommandName { get; set; } = EnchantmentCommand.GobleName;

        public async Task Excute(SocketAutocompleteInteraction interaction)
        {
            List<AutocompleteResult> results = [];
            string itemStr = interaction.Data.Options.FirstOrDefault(x => x.Name == "物品")?.Value as string;
            //await CheckEnchantment(results, keyword);
            await CheckItem(results, itemStr);
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
            if (string.IsNullOrWhiteSpace(keyword)) return;

            ItemSearchResponseDto itemResponseDto = await itemHelper.GetItemsAsync(keyword);
            foreach (Item item in itemResponseDto.Data.Items.Take(25))
            {
                string autocompleteName = $"{PrefixItem} {item.TextName1}";
                string autocompleteValue = $"{PrefixItem} {item.TextName1}";
                results.Add(new(autocompleteName, autocompleteValue));
            }
        }
    }
}
