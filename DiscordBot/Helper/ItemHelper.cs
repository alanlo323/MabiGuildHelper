using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Runtime.Internal.Util;
using DiscordBot.DataObject;
using DiscordBot.Extension;
using DiscordBot.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using RestSharp;

namespace DiscordBot.Helper
{
    public class ItemHelper(ILogger<ItemHelper> logger, DataScrapingHelper dataScrapingHelper)
    {
        public const string BaseAddress = "https://mabinogi.io";
        public const string SearchEndpoint = "napi/items/search";
        public const string ProductionEndpoint = "napi/items/production";
        public const string HtmlEndpoint = "napi/items/";
        public const string CacheDbName = "ItemsCache";
        public const string ItemScreenshotSelector = "#__next > div > div > div.mabinogi-io-main-wrapper > article > div > div > div > div.MuiGrid-root.MuiGrid-item.MuiGrid-grid-xs-12.MuiGrid-grid-sm-12.MuiGrid-grid-md-4.MuiGrid-grid-lg-3 > div > div.MabinogiItemBox_mabinogi_item_box__10Hah";

        public async Task<ItemSearchResponseDto> GetItemsAsync(string keyword, bool withScreenshot = false, bool withProductionInfo = false, bool withDetail = false)
        {
            try
            {
                string name = GetItemName(keyword);
                if (string.IsNullOrWhiteSpace(name)) return new() { Data = new() { Total = 0, Items = [] } };

                Dictionary<string, ItemSearchResponseDto> db = RuntimeDbUtil.GetRuntimeDb<string, ItemSearchResponseDto>(CacheDbName);

                if (!db.TryGetValue(name, out ItemSearchResponseDto responseObj))
                {
                    responseObj = await SearchItem(name);
                    foreach (Item item in responseObj.Data.Items)
                    {
                        item.TextDesc1 = item.TextDesc1.Replace("\n", Environment.NewLine);
                    }
                    db[name] = responseObj;
                }

                //   if only one item found
                if (responseObj?.Data?.Total == 1)
                {
                    foreach (Item item in responseObj.Data.Items)
                    {
                        if (withScreenshot && item.ItemFullImageBase64 == default)
                        {
                            string screenshotBase64 = await dataScrapingHelper.GetElementScreeshotBase64Async(item.Url, ItemScreenshotSelector);
                            item.ItemFullImageBase64 = screenshotBase64;
                        }
                        if (withProductionInfo && item.Production == default)
                        {
                            ItemProductionResponseDto itemProductionResponseDto = await GetItemProduction(item);
                            item.Production = itemProductionResponseDto.Data.Production;
                        }
                        if (withDetail && item.Production == default)
                        {
                            List<string> itemTags = await GetItemTag(item);
                            item.Tags = itemTags;
                        }
                    }
                }

                return responseObj!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get Item: {keyword}. Please try another method. [{ex.Message}]");
            }
        }

        public static string GetItemName(string input)
        {
            string output = input;
            string strToRemove = "瑪奇物品";
            foreach (char c in strToRemove) output = output.Replace(c.ToString(), string.Empty);
            output = output
                .Replace(" ", string.Empty)
                .Replace("Item", string.Empty, StringComparison.CurrentCultureIgnoreCase)
                .Split("/")[0]
                .Trim()
                ;
            return output;
        }

        private async Task<ItemSearchResponseDto> SearchItem(string name)
        {
            try
            {
                logger.LogInformation($"Calling {BaseAddress}/{SearchEndpoint} to search for keyword: {name}");

                ItemSearchRequestDtoQ[] requestDtoQ = [
                    new () {
                            Seq= 1,
                            Mode= "name",
                            Val= name
                        }
                    ];
                ItemSearchRequestDto requestDto = new()
                {
                    Q = requestDtoQ.SerializeWithNewtonsoft()
                };

                RestClient client = new(BaseAddress);
                RestRequest request = new(SearchEndpoint, method: Method.Post)
                {
                    RequestFormat = DataFormat.Json
                };
                request.AddStringBody(requestDto.SerializeWithNewtonsoft(), DataFormat.Json);
                var response = await client.PostAsync(request);
                if (!response.IsSuccessStatusCode) throw new Exception(response.Content);

                ItemSearchResponseDto responseObj = response.Content.DeserializeWithNewtonsoft<ItemSearchResponseDto>()!;
                return responseObj;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<ItemProductionResponseDto> GetItemProduction(Item item)
        {
            try
            {
                logger.LogInformation($"Calling {BaseAddress}/{ProductionEndpoint} to get production info for item: {item.Id} ({item.TextName1})");

                ItemProductionRequestDto requestDto =
                    new()
                    {
                        id = item.Id.ToString()
                    };

                RestClient client = new(BaseAddress);
                RestRequest request = new(ProductionEndpoint, method: Method.Post)
                {
                    RequestFormat = DataFormat.Json
                };
                request.AddStringBody(requestDto.SerializeWithNewtonsoft(), DataFormat.Json);
                var response = await client.PostAsync(request);
                if (!response.IsSuccessStatusCode) throw new Exception(response.Content);

                ItemProductionResponseDto responseObj = response.Content.DeserializeWithNewtonsoft<ItemProductionResponseDto>()!;
                return responseObj;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<List<string>> GetItemTag(Item item)
        {
            try
            {
                string html = await GetItemHtml(item);

                string tagStr = MatchRegex(html, "span class=\"meilisearch-items-lvl1\" style=\"display:none\"", "span").FirstOrDefault();
                List<string> tags = [.. tagStr?.Split("/").Select(s => s.Trim())];

                return tags;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<string> GetItemHtml(Item item)
        {
            try
            {
                logger.LogInformation($"Calling {BaseAddress}/{HtmlEndpoint}/{item.Id} to get production info for item: {item.Id} ({item.TextName1})");

                RestClient client = new(BaseAddress);
                RestRequest request = new($"{ProductionEndpoint}/{item.Id}", method: Method.Get);
                var response = await client.GetAsync(request);
                return !response.IsSuccessStatusCode ? throw new Exception(response.Content) : response.Content;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private List<string> MatchRegex(string input, string fieldNamePre, string? fieldNamePost = default)
        {
            string pattern = $"<{fieldNamePre}>[\\s\\S]*?</{(fieldNamePost == default ? fieldNamePre : fieldNamePost)}>";
            List<string> result = new List<string>();
            MatchCollection matches = Regex.Matches(input, pattern);

            foreach (var match in matches)
            {
                result.Add(match.ToString().Replace($"<{fieldNamePre}>", string.Empty).Replace($"</{(fieldNamePost == default ? fieldNamePre : fieldNamePost)}>", string.Empty));
            }

            return result;
        }
    }
}
