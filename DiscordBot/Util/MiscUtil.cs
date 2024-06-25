using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Image = System.Drawing.Image;
using PuppeteerSharp;
using System.Diagnostics;

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

        /// <summary>
        /// This function will check if the value stay unchanged with specify delay and return the confirmed value.
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="count">The number to check.</param>
        /// <param name="func">Function to call to get the newest result.</param>
        /// <param name="delay">Delay in millisecond.</param>
        /// <returns></returns>
        public static T WaitUntilValueConfirmed<T>(int count, int delay, Func<T> func)
        {
            T value = default;
            do
            {
                Task.Delay(delay).Wait();
                var resultSnapshot = func.Invoke();
                if (Equals(value, resultSnapshot)) count--;
                value = resultSnapshot;
            } while (count > 0);
            return value;
        }

        public static async Task DownloadFileAsync(string url, string destinationPath)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using FileStream fileStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream);
        }

        /// <summary>
        /// Launch the application with some options set.
        /// </summary>
        public static void LaunchCommandLineApp(FileInfo fileInfo, string arguments)
        {
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = true,
                WorkingDirectory = fileInfo?.Directory?.FullName,
                FileName = fileInfo?.Name,
                Arguments = arguments
            };

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using Process exeProcess = Process.Start(startInfo)!;
                exeProcess.WaitForExit(5 * 1000);
            }
            catch
            {
                // Log error.
            }
        }
    }
}
