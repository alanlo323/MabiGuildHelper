using System;
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
    public class ItemHelper(ILogger<ItemHelper> logger)
    {
        public const string BaseAddress = "https://mabinogi.io";
        public const string Endpoint = "napi/items/search";
        public const string CacheDbName = "ItemsCache";

        public async Task<ItemResponseDto> GetItemAsync(string keyword)
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
                    responseObj = response.Content.DeserializeWithNewtonsoft<ItemResponseDto>();
                    db[name] = responseObj!;
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
