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
    public class EnchantmentHelper(ILogger<EnchantmentHelper> logger)
    {
        public const string BaseAddress = "https://mabinogi.io";
        public const string Endpoint = "napi/enchantments/search";
        public const string CacheDbName = "EnchantmentsCache";

        public async Task<EnchantmentResponseDto> GetEnchantmentsAsync(string keyword)
        {
            try
            {
                string name = keyword;
                string strToRemove = "魔力賦予卷軸";
                foreach (char c in strToRemove) name = name.Replace(c.ToString(), string.Empty);
                name = name!
                    .Replace(" ", string.Empty)
                    .Replace("Enchant", string.Empty)
                    .Replace("Enchantment", string.Empty)
                    .Split("/")[0]
                    .Trim()
                    ;
                if (string.IsNullOrWhiteSpace(name)) return new() { Data = new() { Total = 0, Enchantments = [] } };

                Dictionary<string, EnchantmentResponseDto> db = RuntimeDbUtil.GetRuntimeDb<string, EnchantmentResponseDto>(CacheDbName);

                if (!db.TryGetValue(name, out EnchantmentResponseDto responseObj))
                {
                    logger.LogInformation($"Calling {BaseAddress}/{Endpoint} to search for keyword: {name}");

                    EnchantmentRequestDtoQ[] requestDtoQ = [
                        new () {
                            Seq= 1,
                            Mode= "name",
                            Val= name
                        }
                        ];
                    EnchantmentRequestDto requestDto = new()
                    {
                        Q = requestDtoQ.ToJsonString()
                    };

                    RestClient client = new(BaseAddress);
                    RestRequest request = new(Endpoint, method: Method.Post)
                    {
                        RequestFormat = DataFormat.Json
                    };
                    request.AddStringBody(requestDto.ToJsonString(), DataFormat.Json);
                    var response = await client.PostAsync(request);
                    if (!response.IsSuccessStatusCode) throw new Exception(response.Content);
                    responseObj = JsonConvert.DeserializeObject<EnchantmentResponseDto>(response.Content);
                    db[name] = responseObj!;
                }

                return responseObj!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Please try another method. [{ex.Message}]");
            }
        }
    }
}
