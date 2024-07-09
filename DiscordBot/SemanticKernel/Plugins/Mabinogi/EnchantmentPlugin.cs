// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using DiscordBot.SemanticKernel.Core.Text;
using Microsoft.SemanticKernel.Text;
using DiscordBot.SemanticKernel.Plugins.Writer.Summary;
using System.Text;
using DiscordBot.Extension;
using System.Net.Http.Json;
using System.Drawing;
using System.Net.Http.Headers;
using DocumentFormat.OpenXml.Wordprocessing;
using RestSharp;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using DiscordBot.DataObject;
using DiscordBot.Helper;

namespace DiscordBot.SemanticKernel.Plugins.Mabinogi;
/// <summary>
/// Semantic plugin that enables conversations summarization.
/// </summary>
public class EnchantmentPlugin(EnchantmentHelper enchantmentHelper)
{

    /// <summary>
    /// Given a long conversation transcript, summarize the conversation.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Get Mabinogi Enchantment(魔力賦予/魔賦) detail.")]
    public async Task<string> GetEnchantmentInfoAsync(
        [Description("Name of the enchantment")] string name,
        Kernel kernel)
    {
        try
        {
            var responseObj = await enchantmentHelper.GetEnchantmentsAsync(name);
            if (responseObj?.Data.Total < 1) throw new Exception($"There is no Enchantment related to {name}");
            string responseContent = responseObj!.ToString();
            return responseContent;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

}
