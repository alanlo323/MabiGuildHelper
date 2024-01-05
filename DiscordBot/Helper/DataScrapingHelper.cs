using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.DataObject;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace DiscordBot.Helper
{
    public class DataScrapingHelper(ILogger<DataScrapingHelper> logger, AppDbContext appDbContext, DatabaseHelper databaseHelper)
    {
        public const string MabinogiBaseUrl = "https://mabinogi.beanfun.com";
        public const string MabinogiNewsPath = $"{MabinogiBaseUrl}/News";

        public async Task<MabinogiNewsResult> GetMabinogiNews()
        {
            logger.LogInformation("Loading news");

            using BrowserFetcher _browserFetcher = new();
            // Download chrome (headless) browser (first time takes a while).
            await _browserFetcher.DownloadAsync();

            // Launch the browser and set the given html.
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                DefaultViewport = null,
                //Args = [$"--start-maximized"],
                Args = [$"--window-size=450,450"],
            });
            await using var maximizedBrowser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false,
                DefaultViewport = null,
                Args = [$"--start-maximized"],
            });

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
            List<News> newsFromWebsite = [];
            foreach (string activityHtml in activitiesHtml)
            {
                newsFromWebsite.Add(new()
                {
                    Title = activityHtml.Split("item-title\">")[1].Split("</")[0].Trim(),
                    Url = activityHtml.Split("href=\"")[1].Split("\"")[0].Trim().Replace("&amp;", "&"),
                    ImageUrl = activityHtml.Split("background-image: url(&quot;")[1].Split(");")[0].Trim(),
                    PublishDate = DateTime.Parse(activityHtml.Split("item-time\">")[1].Split("</")[0].Trim()),
                });
            }

            int totalNews = newsFromWebsite.Count;
            int loadedNews = 0;

            await Parallel.ForEachAsync(newsFromWebsite, async (news, cancellationToken) =>
              {
                  if (cancellationToken.IsCancellationRequested) return;

                  await using var newsContentPage = await browser.NewPageAsync();
                  await UpdateContent(news, newsContentPage);

                  if (news.Content.Length > 200)
                  {
                      Thread newThread = new(async () =>
                      {
                          await using var newsMaximizedContentPage = await maximizedBrowser.NewPageAsync();
                          await UpdateContent(news, newsMaximizedContentPage);

                          loadedNews++;
                      });
                      newThread.Start();
                  }
                  else
                  {
                      loadedNews++;
                  }
                  logger.LogInformation($"News content updated: {news.Title}");
              });

            while (loadedNews < totalNews)
            {
                Thread.Sleep(100);
            }

            logger.LogInformation($"All News content updated");

            News tempNews = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), newsFromWebsite[0].Url } });
            tempNews.PublishDate = DateTime.Now;
            News tempNews1 = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), newsFromWebsite[1].Url } });
            tempNews1.PublishDate = DateTime.Now;
            News tempNews2 = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), newsFromWebsite[2].Url } });
            tempNews2.PublishDate = DateTime.Now;
            await databaseHelper.SaveChange();

            var sameKeyNews = appDbContext.News.ToList().Where(x => newsFromWebsite.Any(y => y.Url == x.Url)).ToList();
            var updatedNews = sameKeyNews.Where(x => newsFromWebsite.Any(y => y.Url == x.Url && !y.Equals(x))).ToList();
            var newNews = newsFromWebsite.Where(x => !sameKeyNews.Any(y => y.Url == x.Url)).ToList();

            foreach (var newsToUpdate in updatedNews)
            {
                var news = newsFromWebsite.Where(x => x.Url == newsToUpdate.Url).Single();
                newsToUpdate.Title = news.Title;
                newsToUpdate.ImageUrl = news.ImageUrl;
                newsToUpdate.PublishDate = news.PublishDate;
                newsToUpdate.Content = news.Content;
                newsToUpdate.Base64Snapshot = news.Base64Snapshot;
            }
            await appDbContext.News.AddRangeAsync(newNews);
            await appDbContext.SaveChangesAsync();

            var result = await activityElementHandle.ScreenshotDataAsync(
                new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                }
            );

            await browser.CloseAsync();

            logger.LogInformation("News refreshed");

            return new()
            {
                NewNews = newNews,
                UpdatedNews = updatedNews,
                LoadedNews = newsFromWebsite,
            };
        }

        private async Task UpdateContent(News news, IPage page)
        {
            try
            {
                await page.GoToAsync($"{MabinogiBaseUrl}/{news.Url}", WaitUntilNavigation.Networkidle0);

                var knowElementQuery = ".cookie-bar-know";
                await page.WaitForSelectorAsync(knowElementQuery);
                var knowElementHandle = await page.QuerySelectorAsync(knowElementQuery);
                await knowElementHandle.ClickAsync();
            }
            catch (Exception)
            {
            }

            try
            {
                var navElementQuery = ".navigation-bar";
                await page.WaitForSelectorAsync(navElementQuery);
                await page.EvaluateExpressionAsync($"document.querySelector('{navElementQuery}').remove();");

                var bfActionBarElementQuery = "#BF_divActionBar";
                await page.WaitForSelectorAsync(bfActionBarElementQuery);
                await page.EvaluateExpressionAsync($"document.querySelector('{bfActionBarElementQuery}').remove();");
            }
            catch (Exception)
            {
            }

            var contentElementQuery = ".news-inside-content";
            await page.WaitForSelectorAsync(contentElementQuery);
            var contentElementHandle = await page.QuerySelectorAsync(contentElementQuery);
            var contentInnerText = await contentElementHandle.GetPropertyAsync("innerText");

            var contentElementSreenshot = await contentElementHandle.ScreenshotDataAsync(
                new ScreenshotOptions
                {
                    Type = ScreenshotType.Png,
                }
            );
            var contentElementImage = Image.FromStream(new MemoryStream(contentElementSreenshot));

            news.Content = contentInnerText.RemoteObject.Value.ToString()
                .Replace(news.Title, string.Empty)
                .Replace($"{news.PublishDate:yyyy/MM/dd}", string.Empty)
                ;
            news.Base64Snapshot = ImageUtil.ImageToBase64(contentElementImage);

            await page.CloseAsync();
        }
    }
}
