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

namespace DiscordBot.SemanticKernel.Plugins.Mabinogi;
/// <summary>
/// Semantic plugin that enables conversations summarization.
/// </summary>
public class EnchantmentPlugin
{

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationSummaryPlugin"/> class.
    /// </summary>
    public EnchantmentPlugin()
    {
    }

    /// <summary>
    /// Given a long conversation transcript, summarize the conversation.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Get Mabinogi Enchantment detail.")]
    public async Task<string> GetEnchantmentInfoAsync(
        [Description("Name of the enchantment")] string name,
        Kernel kernel)
    {
        try
        {
            string baseAddress = "https://mabinogi.io";
            string endpoint = "napi/enchantments/search";
            var result = await PerformPostRequestAsync(baseAddress, endpoint, name);
            return result;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<string> PerformPostRequestAsync(string baseAddress, string endpoint, string name)
    {
        try
        {
            RequestDtoQ[] requestDtoQ = [
                new () {
                    Seq= 1,
                    Mode= "name",
                    Val= name
                }
            ];
            RequestDto requestDto = new()
            {
                Q = requestDtoQ.ToJsonString()
            };

            RestClient client = new(baseAddress);
            RestRequest request = new(endpoint, method: Method.Post)
            {
                RequestFormat = DataFormat.Json
            };
            request.AddStringBody(requestDto.ToJsonString(), DataFormat.Json);
            var response = await client.PostAsync(request);
            if (!response.IsSuccessStatusCode) throw new Exception(response.Content);
            ResponseDto responseObj = JsonConvert.DeserializeObject<ResponseDto>(response.Content);
            if (responseObj?.Data.Total < 1) throw new Exception($"There is no Enchantment related to {name}");
            string responseContent = responseObj!.ToString();
            return responseContent;
        }
        catch (Exception ex)
        {
            throw new Exception($"Please try another method. [{ex.Message}]");
        }
    }

#pragma warning disable IDE1006 // 命名樣式
#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
    public class RequestDto
    {
        [JsonProperty("q")]
        public string Q { get; set; }
    }

    public class RequestDtoQ
    {
        [JsonProperty("seq")]
        public int? Seq { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("val")]
        public string Val { get; set; }
    }

    public class ResponseDto
    {
        [JsonProperty("data")]
        public Data Data { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            foreach (Enchantment enchantment in Data.Enchantments)
            {
                stringBuilder.AppendLine($"{enchantment.ToString()}");
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }
    }

    public class Data
    {
        [JsonProperty("total")]
        public int? Total { get; set; }

        [JsonProperty("enchantments")]
        public List<Enchantment> Enchantments { get; set; }
    }

    public class Enchantment
    {
        [JsonProperty("io_id")]
        public int? IoId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("LocalName")]
        public string LocalName { get; set; }

        [JsonProperty("LocalName2")]
        public string LocalName2 { get; set; }

        [JsonProperty("allow_item_desc")]
        public string AllowItemDesc { get; set; }

        [JsonProperty("allow_item_count")]
        public int? AllowItemCount { get; set; }

        [JsonProperty("Usage")]
        public string Usage { get; set; }

        [JsonProperty("Level")]
        public string Level { get; set; }

        [JsonProperty("IsAlwaysSuccess")]
        public string IsAlwaysSuccess { get; set; }

        [JsonProperty("IsIgnoreLevel")]
        public string IsIgnoreLevel { get; set; }

        [JsonProperty("io_optionlist_all")]
        public List<IoOptionlistAll> IoOptionlistAll { get; set; }

        [JsonProperty("io_is_personalize")]
        public bool? IoIsPersonalize { get; set; }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"{LocalName} / {Name}");
            stringBuilder.AppendLine($"[{(Usage == "0" ? "接頭" : "接尾")}] [等級 {(16 - Convert.ToInt32(Level)).ToString("X")}]{(IoIsPersonalize == true ? " [專用化]" : string.Empty)}");
            stringBuilder.AppendLine($"適用部位: {AllowItemDesc}");
            stringBuilder.AppendLine();
            foreach (IoOptionlistAll option in IoOptionlistAll)
            {
                stringBuilder.AppendLine($"{(string.IsNullOrEmpty(option.Condition) ? string.Empty : $"{option.ConditionDesc} ")}{option.EffectFullDesc}");
            }
            return stringBuilder.ToString();
        }
    }

    public class IoOptionlistAll
    {
        [JsonProperty("condition")]
        public string Condition { get; set; }

        [JsonProperty("condition_desc")]
        public string ConditionDesc { get; set; }

        [JsonProperty("effect")]
        public string Effect { get; set; }

        [JsonProperty("effect_type")]
        public string EffectType { get; set; }

        [JsonProperty("effect_type_desc")]
        public string EffectTypeDesc { get; set; }

        [JsonProperty("effect_min_val")]
        public object EffectMinVal { get; set; }

        [JsonProperty("effect_max_val")]
        public object EffectMaxVal { get; set; }

        [JsonProperty("effect_val")]
        public string EffectVal { get; set; }

        [JsonProperty("effect_final_val")]
        public string EffectFinalVal { get; set; }

        [JsonProperty("effect_full_desc")]
        public string EffectFullDesc { get; set; }

        [JsonProperty("effect_suffix")]
        public string EffectSuffix { get; set; }

        [JsonProperty("good_bad")]
        public string GoodBad { get; set; }
    }
#pragma warning restore IDE1006 // 命名樣式
#pragma warning restore CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
}
