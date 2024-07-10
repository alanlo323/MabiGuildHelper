using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordBot.DataObject
{
#pragma warning disable IDE1006 // 命名樣式
#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ItemRequestDto
    {
        [JsonProperty("q")]
        public string Q { get; set; }
    }
    public class ItemRequestDtoQ
    {
        [JsonProperty("seq")]
        public int? Seq { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("val")]
        public string Val { get; set; }
    }

    public class ItemResponseDto
    {
        [JsonProperty("data")]
        public ItemResponseData Data { get; set; }
    }

    public class ItemResponseData
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        [JsonProperty("_id")]
        public int Id { get; set; }

        [JsonProperty("text_name1")]
        public string TextName1 { get; set; }

        [JsonProperty("text_desc1")]
        public string TextDesc1 { get; set; }

        [JsonProperty("file_invimage")]
        public string FileInvimage { get; set; }

        public string Url { get => $"https://mabinogi.io/items/{Id}"; }
        public string ImageUrl { get => $"https://mabinogi.io/napi/image/items/{Id}.png?dc=1"; }
    }
#pragma warning restore IDE1006 // 命名樣式
#pragma warning restore CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
}
