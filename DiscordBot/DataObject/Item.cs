using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiscordBot.Util;
using Irony.Parsing;
using Newtonsoft.Json;

namespace DiscordBot.DataObject
{
#pragma warning disable IDE1006 // 命名樣式
#pragma warning disable CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
    // Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class ItemProductionRequestDto
    {
        [JsonProperty("id")]
        public string id { get; set; }
    }

    public class ItemProductionResponseDto
    {
        [JsonProperty("data")]
        public ItemProductionResponseData Data { get; set; }
    }

    public class ItemProductionResponseData
    {
        [JsonProperty("production")]
        public List<Production> Production { get; set; }
    }

    public class EssentialsArr
    {
        [JsonProperty("desc")]
        public string Desc { get; set; }

        [JsonProperty("qty")]
        public int Qty { get; set; }

        [JsonProperty("is_multiple_items_match")]
        public bool IsMultipleItemsMatch { get; set; }

        [JsonProperty("items")]
        public List<ProductItem> Items { get; set; }
    }

    public class ProductItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Production
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("ProductionId")]
        public string ProductionId { get; set; }

        [JsonProperty("ProductionType")]
        public string ProductionType { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("ProductItemId")]
        public string ProductItemId { get; set; }

        [JsonProperty("ProductionCount")]
        public string ProductionCount { get; set; }

        [JsonProperty("Difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("MerchantExp")]
        public string MerchantExp { get; set; }

        [JsonProperty("PrimeTool")]
        public string PrimeTool { get; set; }

        [JsonProperty("DurabilityDownPrime")]
        public string DurabilityDownPrime { get; set; }

        [JsonProperty("DurabilityDownSecond")]
        public string DurabilityDownSecond { get; set; }

        [JsonProperty("EssentialDesc")]
        public string EssentialDesc { get; set; }

        [JsonProperty("Essentials")]
        public string Essentials { get; set; }

        [JsonProperty("ManaRequired")]
        public string ManaRequired { get; set; }

        [JsonProperty("MaxProduction")]
        public string MaxProduction { get; set; }

        [JsonProperty("MaxAutoProduction")]
        public string MaxAutoProduction { get; set; }

        [JsonProperty("ClearType")]
        public string ClearType { get; set; }

        [JsonProperty("MaxBonus")]
        public string MaxBonus { get; set; }

        [JsonProperty("Desc")]
        public string Desc { get; set; }

        [JsonProperty("SuccessRate_0")]
        public string SuccessRate0 { get; set; }

        [JsonProperty("SuccessRate_1")]
        public string SuccessRate1 { get; set; }

        [JsonProperty("SuccessRate_2")]
        public string SuccessRate2 { get; set; }

        [JsonProperty("SuccessRate_3")]
        public string SuccessRate3 { get; set; }

        [JsonProperty("SuccessRate_4")]
        public string SuccessRate4 { get; set; }

        [JsonProperty("SuccessRate_5")]
        public string SuccessRate5 { get; set; }

        [JsonProperty("SuccessRate_6")]
        public string SuccessRate6 { get; set; }

        [JsonProperty("SuccessRate_7")]
        public string SuccessRate7 { get; set; }

        [JsonProperty("SuccessRate_8")]
        public string SuccessRate8 { get; set; }

        [JsonProperty("SuccessRate_9")]
        public string SuccessRate9 { get; set; }

        [JsonProperty("SuccessRate_10")]
        public string SuccessRate10 { get; set; }

        [JsonProperty("SuccessRate_11")]
        public string SuccessRate11 { get; set; }

        [JsonProperty("SuccessRate_12")]
        public string SuccessRate12 { get; set; }

        [JsonProperty("SuccessRate_13")]
        public string SuccessRate13 { get; set; }

        [JsonProperty("SuccessRate_14")]
        public string SuccessRate14 { get; set; }

        [JsonProperty("SuccessRate_15")]
        public string SuccessRate15 { get; set; }

        [JsonProperty("SuccessRate_16")]
        public string SuccessRate16 { get; set; }

        [JsonProperty("SuccessRate_17")]
        public string SuccessRate17 { get; set; }

        [JsonProperty("SuccessRate_18")]
        public string SuccessRate18 { get; set; }

        [JsonProperty("ToolMissmatchMsg")]
        public string ToolMissmatchMsg { get; set; }

        [JsonProperty("SuccessRateBonusInRain")]
        public string SuccessRateBonusInRain { get; set; }

        [JsonProperty("Generation")]
        public string Generation { get; set; }

        [JsonProperty("Season")]
        public string Season { get; set; }

        [JsonProperty("io_productionitemid")]
        public int IoProductionitemid { get; set; }

        [JsonProperty("xml_type")]
        public string XmlType { get; set; }

        [JsonProperty("essentials_arr")]
        public List<EssentialsArr> EssentialsArr { get; set; }

        [JsonProperty("xml_type_desc")]
        public string XmlTypeDesc { get; set; }

        [JsonProperty("skill_id")]
        public int SkillId { get; set; }

        [JsonProperty("skill_image")]
        public string SkillImage { get; set; }

        [JsonProperty("skill_image_offset_x")]
        public int SkillImageOffsetX { get; set; }

        [JsonProperty("skill_image_offset_y")]
        public int SkillImageOffsetY { get; set; }

        [JsonProperty("SpecialTalent_desc")]
        public string SpecialTalentDesc { get; set; }

        [JsonProperty("xml_file")]
        public string XmlFile { get; set; }
    }

    public class ItemSearchRequestDto
    {
        [JsonProperty("q")]
        public string Q { get; set; }
    }

    public class ItemSearchRequestDtoQ
    {
        [JsonProperty("seq")]
        public int? Seq { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("val")]
        public string Val { get; set; }
    }

    public class ItemSearchResponseDto
    {
        [JsonProperty("data")]
        public ItemSearchResponseData Data { get; set; }
    }

    public class ItemSearchResponseData
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
        public string ItemFullImageBase64 { get; set; }

        [NotMapped]
        private FileInfo? _snapshotTempFile;

        [NotMapped]
        public FileInfo SnapshotTempFile
        {
            get
            {
                if (_snapshotTempFile == null)
                {
                    string tempFilePath = Path.GetTempFileName().Replace("tmp", "png");
                    _snapshotTempFile = new FileInfo(tempFilePath);
                }

                try
                {
                    Image image;
                    if (string.IsNullOrEmpty(ItemFullImageBase64))
                    {
                        // save a blank image
                        image = new Bitmap(1, 1);
                    }
                    else
                    {
                        image = ImageUtil.Base64ToImage(ItemFullImageBase64);
                    }
                    image.Save(_snapshotTempFile.FullName);
                }
                catch (Exception) { }

                return _snapshotTempFile;
            }
        }

        [NotMapped]
        public List<Production> Production { get; set; }

        public override string ToString() => ToString(true);

        public string ToString(bool includeName)
        {
            StringBuilder stringBuilder = new();
            if (includeName) stringBuilder.AppendLine($"{TextName1}");
            stringBuilder.AppendLine($"{TextDesc1}");
            Production production = Production.FirstOrDefault();
            if (production != default)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"**生產資訊** : {production.XmlTypeDesc} {(string.IsNullOrWhiteSpace(production.ToolMissmatchMsg) ? default : $"({production.ToolMissmatchMsg})")}");
                stringBuilder.AppendLine($"{production.Desc}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"**材料** :");
                foreach (EssentialsArr essentialsArr in production.EssentialsArr)
                {
                    stringBuilder.Append($"{string.Join(", ", essentialsArr.Items.Select(item => $"[{item.Name}](https://mabinogi.io/items/{item.Id})"))}");
                    stringBuilder.AppendLine($" x {essentialsArr.Qty}");
                }
            }
            return stringBuilder.ToString();
        }
    }
#pragma warning restore IDE1006 // 命名樣式
#pragma warning restore CS8618 // 退出建構函式時，不可為 Null 的欄位必須包含非 Null 值。請考慮宣告為可為 Null。
}
