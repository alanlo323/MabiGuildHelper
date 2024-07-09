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
    public class ChatCommandAutocompleteHandler(EnchantmentHelper enchantmentHelper) : IBaseAutocompleteHandler
    {
        public string CommandName { get; set; } = "chat";

        public async Task Excute(SocketAutocompleteInteraction interaction)
        {
            List<AutocompleteResult> results = [];
            string keyword = interaction.Data.Options.First(x => x.Name == "text").Value as string;
            var responseObj = await enchantmentHelper.GetEnchantmentsAsync(keyword);
            foreach (Enchantment enchantment in responseObj.Data.Enchantments.Take(25))
            {
                //string autocomputeText = $"{oriText?.Replace(keyword, enchantment.LocalName)}";
                string autocomputeName = $"魔力賦予 {enchantment.LocalName} / {enchantment.Name}";
                string autocomputeValue = $"魔力賦予 {enchantment.LocalName}";
                results.Add(new(autocomputeName, autocomputeValue));
            }
            await interaction.RespondAsync(results);
        }
    }
}
