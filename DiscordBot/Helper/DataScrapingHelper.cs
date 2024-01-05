using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace DiscordBot.Helper
{
    public class DataScrapingHelper(ILogger<DataScrapingHelper> logger, AppDbContext appDbContext, DatabaseHelper databaseHelper)
    {
        public const string MabinogiBaseUrl = "https://mabinogi.beanfun.com";
        public const string MabinogiNewsPath = $"{MabinogiBaseUrl}/News";

        public async Task<List<News>> GetNews(string type = null)
        {
            using BrowserFetcher _browserFetcher = new();
            // Download chrome (headless) browser (first time takes a while).
            await _browserFetcher.DownloadAsync();

            // Launch the browser and set the given html.
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            await using var newsPage = await browser.NewPageAsync();
            // get from MabinogiTW official website
            await newsPage.GoToAsync(MabinogiNewsPath, WaitUntilNavigation.Networkidle0);

            var activityElementQuery = ".activity";
            await newsPage.WaitForSelectorAsync(activityElementQuery); // Wait for the selector to load.

            var activityElementHandle = await newsPage.QuerySelectorAsync(activityElementQuery);
            var activityInnerHTML = await activityElementHandle.GetPropertyAsync("innerHTML");
            List<string> activitiesHtml = [];
            foreach (string html in activityInnerHTML.RemoteObject.Value.ToString().Split("</a>"))
            {
                if (!string.IsNullOrWhiteSpace(html)) activitiesHtml.Add($"{html.Trim()}</a>");
            }

            // convert activitiesHtml to News
            List<News> firstPageNews = [];
            foreach (string activityHtml in activitiesHtml)
            {
                firstPageNews.Add(new()
                {
                    Title = activityHtml.Split("item-title\">")[1].Split("</")[0].Trim(),
                    Url = activityHtml.Split("href=\"")[1].Split("\"")[0].Trim().Replace("&amp;", "&"),
                    ImageUrl = activityHtml.Split("background-image: url(&quot;")[1].Split(");")[0].Trim(),
                    PublishDate = DateTime.Parse(activityHtml.Split("item-time\">")[1].Split("</")[0].Trim()),
                });
            }

            await Parallel.ForEachAsync(firstPageNews, async (news, cancellationToken) =>
              {
                  if(cancellationToken.IsCancellationRequested) return;

                  await using var newsContentPage = await browser.NewPageAsync();
                  await newsContentPage.GoToAsync($"{MabinogiBaseUrl}/{news.Url}", WaitUntilNavigation.Networkidle0);

                  var contentElementQuery = ".news-inside-content";
                  await newsContentPage.WaitForSelectorAsync(contentElementQuery);

                  var contentElementHandle = await newsContentPage.QuerySelectorAsync(contentElementQuery);
                  var contentInnerText = await contentElementHandle.GetPropertyAsync("innerText");

                  news.Content = contentInnerText.RemoteObject.Value.ToString();
              });

            News tempNews = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), firstPageNews[0].Url } });
            tempNews.Content = $"{DateTime.Now}";
            await databaseHelper.SaveChange();

            var sameKeyNews = appDbContext.News.ToList().Where(x => firstPageNews.Any(y => y.Url == x.Url));
            var updatedNews = firstPageNews.Where(x => sameKeyNews.Any(y => y.Url == x.Url && !y.Equals(x)));
            var newNews = firstPageNews.Where(x => !sameKeyNews.Any(y => y.Url == x.Url));

            foreach (var news in updatedNews)
            {
                var newsToUpdate = sameKeyNews.Where(x => x.Url == news.Url).Single();
                newsToUpdate.Title = news.Title;
                newsToUpdate.ImageUrl = news.ImageUrl;
                newsToUpdate.PublishDate = news.PublishDate;
                newsToUpdate.Content = news.Content;
            }
            await appDbContext.News.AddRangeAsync(newNews);
            await appDbContext.SaveChangesAsync();

            logger.LogInformation("News loaded");

            var result = await activityElementHandle.ScreenshotDataAsync(
                new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                }
            );

            await browser.CloseAsync();

            var image = Image.FromStream(new MemoryStream(result));
            // save image to file, for debug
            string tempPath = Path.GetTempFileName().Replace("tmp", "jpg");
            image.Save(tempPath);
            return firstPageNews;
        }
    }
}
