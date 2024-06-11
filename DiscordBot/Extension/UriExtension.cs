using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Extension
{
    public static class UriExtension
    {
        public static async Task<bool> IsImageUrl(this Uri url)
        {
            try
            {
                using HttpClient client = new();
                HttpRequestMessage request = new(HttpMethod.Head, url);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string contentType = response.Content.Headers.ContentType.MediaType;
                    return contentType.StartsWith("image/");
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                // If any error occurs (e.g., the request is blocked or the URL is not valid), assume it's not an image.
                return false;
            }
        }

    }
}
