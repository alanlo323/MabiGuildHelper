using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;
using PuppeteerSharp;

namespace DiscordBot.Util
{
    internal class MiscUtil
    {
        public async static Task<Image> ConvertHtmlToImage(string html)
        {
            BrowserFetcher _browserFetcher = new();
            // Download chrome (headless) browser (first time takes a while).
            await _browserFetcher.DownloadAsync();

            // Launch the browser and set the given html.
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html);

            // Select the element and take a screen-shot, or just use the page, for example: page.ScreenshotDataAsync()
            var elementQuery = "#dungeons";
            await page.WaitForSelectorAsync(elementQuery, new WaitForSelectorOptions { Timeout = 2000 }); // Wait for the selector to load.

            var elementHandle = await page.QuerySelectorAsync(elementQuery); // Declare a variable with an ElementHandle.
            var result = await elementHandle.ScreenshotDataAsync(
                new ElementScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                }
            );

            await browser.CloseAsync();

            return Image.FromStream(new MemoryStream(result));
        }

        public static string GetValidFileName(string url)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string sanitizedUrl = string.Join("-", url.Split(invalidChars.ToCharArray()));
            string[] invalidChars2 = { "<", ">", ":", "\"", "/", "\\", "|", "?", "*", " ", "#", "=" };
            foreach (var invalidChar in invalidChars2)
            {
                sanitizedUrl = sanitizedUrl.Replace(invalidChar, "-");
            }
            return sanitizedUrl;
        }
    }
}
