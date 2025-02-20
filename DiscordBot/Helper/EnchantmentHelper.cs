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
                string enchantmentName = GetEnchantmentName(keyword);
                if (string.IsNullOrWhiteSpace(enchantmentName)) return new() { Data = new() { Total = 0, Enchantments = [] } };

                Dictionary<string, EnchantmentResponseDto> db = RuntimeDbUtil.GetRuntimeDb<string, EnchantmentResponseDto>(CacheDbName);

                if (!db.TryGetValue(enchantmentName, out EnchantmentResponseDto responseObj))
                {
                    logger.LogInformation($"Calling {BaseAddress}/{Endpoint} to search for keyword: {enchantmentName}");

                    EnchantmentRequestDtoQ[] requestDtoQ = [
                        new () {
                            Seq= 1,
                            Mode= "name",
                            Val= enchantmentName
                        }
                        ];
                    EnchantmentRequestDto requestDto = new()
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
                    responseObj = response.Content.DeserializeWithNewtonsoft<EnchantmentResponseDto>();
                    db[enchantmentName] = responseObj!;
                }

                return responseObj!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Please try another method. [{ex.Message}]");
            }
        }

        public static string GetEnchantmentName(string input)
        {
            string output = input;
            string strToRemove = "瑪奇魔力賦予卷軸";
            foreach (char c in strToRemove) output = output.Replace(c.ToString(), string.Empty);
            output = output
                .Replace(" ", string.Empty)
                .Replace("Enchant", string.Empty, StringComparison.CurrentCultureIgnoreCase)
                .Replace("Enchantment", string.Empty, StringComparison.CurrentCultureIgnoreCase)
                .Split("/")[0]
                .Trim()
                ;
            return output;
        }
    }
}
