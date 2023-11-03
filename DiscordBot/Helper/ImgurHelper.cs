using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Configuration;
using DiscordBot.DataEntity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;

namespace DiscordBot.Helper
{
    public class ImgurHelper
    {
        public class ImgurTokenResponse
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public string token_type { get; set; }
            public string scope { get; set; }
            public string refresh_token { get; set; }
            public int account_id { get; set; }
            public string account_username { get; set; }
        }

        public class ImgurImageUploadResponse
        {
            public class ImgurImageUploadData
            {
                public string id { get; set; }
                public string title { get; set; }
                public string description { get; set; }
                public int datetime { get; set; }
                public string type { get; set; }
                public bool? animated { get; set; }
                public int? width { get; set; }
                public int? height { get; set; }
                public int? size { get; set; }
                public int ?views { get; set; }
                public int? bandwidth { get; set; }
                public object vote { get; set; }
                public bool? favorite { get; set; }
                public bool? nsfw { get; set; }
                public string section { get; set; }
                public object account_url { get; set; }
                public int? account_id { get; set; }
                public bool? is_ad { get; set; }
                public bool? in_most_viral { get; set; }
                public bool? has_sound { get; set; }
                public List<object> tags { get; set; }
                public int? ad_type { get; set; }
                public string ad_url { get; set; }
                public bool? in_gallery { get; set; }
                public string deletehash { get; set; }
                public string name { get; set; }
                public string link { get; set; }
            }
            public ImgurImageUploadData data { get; set; }
            public bool? success { get; set; }
            public int? status { get; set; }
        }

        public static readonly string BaseUrl = "https://api.imgur.com";

        ILogger<ImgurHelper> _logger;
        IOptionsSnapshot<ImgurConfig> _imgurConfig;
        string _accessToken;

        private string AccessToken
        {
            get
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    _accessToken = GetAccessToken(_imgurConfig.Value.ClientId, _imgurConfig.Value.ClientSecret, _imgurConfig.Value.RefreshToken).Result;
                }
                return _accessToken;
            }
            set => _accessToken = value;
        }

        public ImgurHelper(ILogger<ImgurHelper> logger, IOptionsSnapshot<ImgurConfig> imgurConfig)
        {
            _logger = logger;
            _imgurConfig = imgurConfig;
        }

        public async Task<string> GetAccessToken(string clientId, string clientSecret, string refreshToken)
        {
            var options = new RestClientOptions(BaseUrl)
            {
                MaxTimeout = -1,
            };
            var client = new RestClient(options);
            var request = new RestRequest("/oauth2/token", Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            request
                .AddParameter("refresh_token", refreshToken)
                .AddParameter("client_id", clientId)
                .AddParameter("client_secret", clientSecret)
                .AddParameter("grant_type", "refresh_token")
                ;
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                return JsonConvert.DeserializeObject<ImgurTokenResponse>(response.Content).access_token;
            }
            else
            {
                _logger.LogError($"GetAccessToken failed: {response.Content}");
                return null;
            }
        }

        public async Task<string> UploadImage(Image image)
        {
            // image to base64
            var options = new RestClientOptions(BaseUrl)
            {
                MaxTimeout = -1,
            };
            var client = new RestClient(options);
            var request = new RestRequest("/3/image", Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            byte[] asd = new byte[0];
            // image to byte[]
            using var ms = new MemoryStream();
            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var imgBase64 = Convert.ToBase64String(ms.ToArray());
            request
                .AddHeader("Authorization", $"Bearer {AccessToken}")
                .AddParameter("image", imgBase64)
                ;
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                // get response object from reponse content
                var responseObject = JsonConvert.DeserializeObject<ImgurImageUploadResponse>(response.Content);
                return responseObject.data.link;
            }
            else
            {
                _logger.LogError($"UploadImage failed: {response.Content}");
                return null;
            }

        }
    }
}
