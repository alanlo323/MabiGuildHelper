﻿using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public const string Endpoint = "napi/items/search";
        public const string CacheDbName = "ItemsCache";

        public async Task<ItemResponseDto> GetItemAsync(string keyword, bool withScreenshot = false)
        {
            try
            {
                string name = GetItemName(keyword);
                if (string.IsNullOrWhiteSpace(name)) return new() { Data = new() { Total = 0, Items = [] } };

                Dictionary<string, ItemResponseDto> db = RuntimeDbUtil.GetRuntimeDb<string, ItemResponseDto>(CacheDbName);

                if (!db.TryGetValue(name, out ItemResponseDto responseObj))
                {
                    logger.LogInformation($"Calling {BaseAddress}/{Endpoint} to search for keyword: {name}");

                    ItemRequestDtoQ[] requestDtoQ = [
                        new () {
                            Seq= 1,
                            Mode= "name",
                            Val= name
                        }
                        ];
                    ItemRequestDto requestDto = new()
                    {
                        Q = requestDtoQ.SerializeWithNewtonsoft()
                    };

                    RestClient client = new(BaseAddress);
                    RestRequest request = new(Endpoint, method: Method.Post)
                    {
                        RequestFormat = DataFormat.Json
                    };
                    request.AddStringBody(requestDto.SerializeWithNewtonsoft(), DataFormat.Json);
                    var response = await client.PostAsync(request);
                    if (!response.IsSuccessStatusCode) throw new Exception(response.Content);
                    responseObj = response.Content.DeserializeWithNewtonsoft<ItemResponseDto>()!;
                    db[name] = responseObj;
                }

                //  Get screenshot if only one item found
                if (withScreenshot && responseObj?.Data?.Total == 1)
                {
                    foreach (Item item in responseObj.Data.Items)
                    {
                        if (item.ItemFullImageBase64 != default) continue;
                        string targetSelector = "#__next > div > div > div.mabinogi-io-main-wrapper > article > div > div > div > div.MuiGrid-root.MuiGrid-item.MuiGrid-grid-xs-12.MuiGrid-grid-sm-12.MuiGrid-grid-md-4.MuiGrid-grid-lg-3 > div > div.MabinogiItemBox_mabinogi_item_box__10Hah";
                        string screenshotBase64 = await dataScrapingHelper.GetElementScreeshotBase64Async(item.Url, targetSelector);
                        item.ItemFullImageBase64 = screenshotBase64;
                    }
                }

                return responseObj!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Please try another method. [{ex.Message}]");
            }
        }

        public string GetItemName(string input)
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
    }
}