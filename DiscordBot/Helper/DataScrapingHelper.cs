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
                Headless = true,
                DefaultViewport = null,
                //Args = [$"--start-maximized"],
                //Args = [$"--window-size=960,540 --force-device-scale-factor=1.5"],
                Args = [$"--window-size=450,450"],
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
                  if (cancellationToken.IsCancellationRequested) return;

                  try
                  {
                      await using var newsContentPage = await browser.NewPageAsync();
                      await newsContentPage.GoToAsync($"{MabinogiBaseUrl}/{news.Url}", WaitUntilNavigation.Networkidle0);

                      try
                      {
                          var knowElementQuery = ".cookie-bar-know";
                          await newsContentPage.WaitForSelectorAsync(knowElementQuery);
                          var knowElementHandle = await newsContentPage.QuerySelectorAsync(knowElementQuery);
                          await knowElementHandle.ClickAsync();
                      }
                      catch (Exception)
                      {
                      }

                      try
                      {
                          var navElementQuery = ".navigation-bar";
                          await newsContentPage.WaitForSelectorAsync(navElementQuery);
                          await newsContentPage.EvaluateExpressionAsync($"document.querySelector('{navElementQuery}').remove();");
                      }
                      catch (Exception)
                      {
                      }

                      var contentElementQuery = ".news-inside-content";
                      await newsContentPage.WaitForSelectorAsync(contentElementQuery);
                      var contentElementHandle = await newsContentPage.QuerySelectorAsync(contentElementQuery);
                      var contentInnerText = await contentElementHandle.GetPropertyAsync("innerText");

                      news.Content = contentInnerText.RemoteObject.Value.ToString();

                      var contentElementSreenshot = await contentElementHandle.ScreenshotDataAsync(
                          new ScreenshotOptions
                          {
                              Type = ScreenshotType.Png,
                          }
                      );
                      var contentElementImage = Image.FromStream(new MemoryStream(contentElementSreenshot));
                      //string tempPath1 = Path.GetTempFileName().Replace("tmp", "jpg");
                      //contentElementImage.Save(tempPath1);

                      news.Base64Snapshot = ImageUtil.ImageToBase64(contentElementImage);
                  }
                  catch (Exception ex)
                  {
                      logger.LogError(ex, ex.Message);
                  }
              });

            News tempNews = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), firstPageNews[0].Url } });
            tempNews.PublishDate = DateTime.Now;
            await databaseHelper.SaveChange();

            var sameKeyNews = appDbContext.News.ToList().Where(x => firstPageNews.Any(y => y.Url == x.Url));
            var updatedNews = sameKeyNews.Where(x => firstPageNews.Any(y => y.Url == x.Url && !y.Equals(x)));
            var newNews = firstPageNews.Where(x => !sameKeyNews.Any(y => y.Url == x.Url));

            foreach (var news in updatedNews)
            {
                var newsToUpdate = sameKeyNews.Where(x => x.Url == news.Url).Single();
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

            //var image = Image.FromStream(new MemoryStream(result));
            //// save image to file, for debug
            //string tempPath = Path.GetTempFileName().Replace("tmp", "jpg");
            //image.Save(tempPath);

            logger.LogInformation("News refreshed");

            return new()
            {
                NewNews = newNews.ToList(),
                UpdatedNews = updatedNews.ToList(),
                LoadedNews = firstPageNews,
            };
        }
    }
}
