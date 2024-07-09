using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordBot.DataObject
{
#pragma warning disable IDE1006 // 命名樣式
#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
    public class EnchantmentRequestDto
    {
        [JsonProperty("q")]
        public string Q { get; set; }
    }

    public class EnchantmentRequestDtoQ
    {
        [JsonProperty("seq")]
        public int? Seq { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("val")]
        public string Val { get; set; }
    }

    public class EnchantmentResponseDto
    {
        [JsonProperty("data")]
        public EnchantmentData Data { get; set; }

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

    public class EnchantmentData
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

        public override string ToString() => ToString(true);

        public string ToString(bool includeName)
        {
            StringBuilder stringBuilder = new();
            if (includeName) stringBuilder.AppendLine($"{LocalName} / {Name}");
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
