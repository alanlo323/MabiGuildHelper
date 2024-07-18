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
        public string CommandName { get; set; } = "chat";

        public async Task Excute(SocketAutocompleteInteraction interaction)
        {
            List<AutocompleteResult> results = [];
            await interaction.RespondAsync(results);
            return;
            string keyword = interaction.Data.Options.First(x => x.Name == "text").Value as string;
            #region Check Enchantment
            EnchantmentResponseDto enchantmentResponseDto = await enchantmentHelper.GetEnchantmentsAsync(keyword);
            foreach (Enchantment enchantment in enchantmentResponseDto.Data.Enchantments.Take(25))
            {
                string autocomputeName = $"魔力賦予 {enchantment.LocalName} / {enchantment.Name}";
                string autocomputeValue = $"魔力賦予 {enchantment.LocalName}";
                results.Add(new(autocomputeName, autocomputeValue));
            }
            #endregion
            #region Check Item
            ItemResponseDto itemResponseDto = await itemHelper.GetItemAsync(keyword);
            foreach (Item item in itemResponseDto.Data.Items.Take(25))
            {
                string autocomputeName = $"物品 {item.TextName1}";
                string autocomputeValue = $"物品 {item.TextName1}";
                results.Add(new(autocomputeName, autocomputeValue));
            }
            #endregion
            results = results.Take(25).ToList();
            await interaction.RespondAsync(results);
        }
    }
}
