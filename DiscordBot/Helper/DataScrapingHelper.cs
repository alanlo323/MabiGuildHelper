using System;
using System.Collections.Concurrent;
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
using DiscordBot.Extension;
using DiscordBot.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Plugins.Web;
using PuppeteerSharp;
using Quartz.Util;

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
            await _browserFetcher.DownloadAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                DefaultViewport = null,
                Args = [$"--window-size=450,450"],
            });
            await using var maximizedBrowser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                DefaultViewport = null,
                Args = [$"--start-maximized"],
            });

            await using var newsPage = await browser.NewPageAsync();
            await newsPage.GoToAsync(MabinogiNewsPath, WaitUntilNavigation.Networkidle0);
            var activityElementQuery = ".activity";
            await newsPage.WaitForSelectorAsync(activityElementQuery);

            var activityElementHandle = await newsPage.QuerySelectorAsync(activityElementQuery);
            var activityInnerHTML = await activityElementHandle.GetPropertyAsync("innerHTML");
            List<string> activitiesHtml = [];
            foreach (string html in activityInnerHTML.RemoteObject.Value.ToString().Split("</a>"))
            {
                if (!string.IsNullOrWhiteSpace(html)) activitiesHtml.Add($"{html.Trim()}</a>");
            }

            List<News> newsFromWebsite = [];
            foreach (string activityHtml in activitiesHtml)
            {
                try
                {
                    newsFromWebsite.Add(new()
                    {
                        Title = activityHtml.Split("item-title\">")[1].Split("</")[0].Trim().MarkDownEscape(),
                        ItemTag = Enum.Parse<ItemTag>(activityHtml.Split("item-tag ")[1].Split("\"")[0].Trim()),
                        Url = activityHtml.Split("href=\"")[1].Split("\"")[0].Trim().Replace("&amp;", "&"),
                        ImageUrl = activityHtml.Split("background-image: url(&quot;")[1].Split(");")[0].Trim(),
                        PublishDate = DateTime.Parse(activityHtml.Split("item-time\">")[1].Split("</")[0].Trim()),
                    });
                }
                catch (Exception) { }
            }

            int totalNews = newsFromWebsite.Count;

            int loadedNews = 0;

            await Parallel.ForEachAsync(newsFromWebsite, async (news, cancellationToken) =>
              {
                  if (cancellationToken.IsCancellationRequested) return;

                  await using var newsContentPage = await browser.NewPageAsync();
                  await UpdateNewsContent(news, newsContentPage);

                  switch (news.Content?.Length)
                  {
                      case > 200:
                          {
                              Thread newThread = new(async () =>
                              {
                                  await using var newsMaximizedContentPage = await maximizedBrowser.NewPageAsync();
                                  await UpdateNewsContent(news, newsMaximizedContentPage);

                                  loadedNews++;
                              });
                              newThread.Start();
                              break;
                          }

                      default:
                          loadedNews++;
                          break;
                  }
                  logger.LogInformation($"News content updated: {news.Title}");
              });

            while (loadedNews < totalNews)
            {
                Thread.Sleep(100);
            }

            logger.LogInformation($"All News content updated");

            if (EnvironmentUtil.IsLocal())
            {
                News tempNews = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), newsFromWebsite[0].Url } });
                tempNews.PublishDate = DateTime.Now;
                News tempNews1 = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), newsFromWebsite[1].Url } });
                tempNews1.PublishDate = DateTime.Now;
                News tempNews2 = await databaseHelper.GetOrCreateEntityByKeys<News>(new() { { nameof(News.Url), newsFromWebsite[2].Url } });
                tempNews2.PublishDate = DateTime.Now;
                await databaseHelper.SaveChange();
            }

            var sameKeyNews = appDbContext.News.ToList().Where(x => newsFromWebsite.Any(y => y.Url == x.Url)).ToList();
            var updatedNews = sameKeyNews.Where(x => newsFromWebsite.Any(y => y.Url == x.Url && !y.Equals(x))).ToList();
            var newNews = newsFromWebsite.Where(x => !sameKeyNews.Any(y => y.Url == x.Url)).ToList();

            foreach (var newsToUpdate in updatedNews)
            {
                var news = newsFromWebsite.Where(x => x.Url == newsToUpdate.Url).Single();
                newsToUpdate.Title = news.Title;
                newsToUpdate.ItemTag = news.ItemTag;
                newsToUpdate.ImageUrl = news.ImageUrl;
                newsToUpdate.PublishDate = news.PublishDate;
                newsToUpdate.Content = news.Content;
                newsToUpdate.Base64Snapshot = news.Base64Snapshot;
            }
            await appDbContext.News.AddRangeAsync(newNews);
            await appDbContext.SaveChangesAsync();

            await browser.CloseAsync();

            logger.LogInformation("News refreshed");

            return new()
            {
                NewNews = newNews,
                UpdatedNews = updatedNews,
                LoadedNews = newsFromWebsite,
            };
        }

        private async Task UpdateNewsContent(News news, IPage page)
        {
            try
            {
                string url = $"{MabinogiBaseUrl}/{news.Url}";
                // check if urk is valid
                if (!Uri.IsWellFormedUriString(news.Url, UriKind.Relative)) return;

                await page.GoToAsync(url, WaitUntilNavigation.Networkidle0);
                try
                {
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

                var contentElementSreenshot = await contentElementHandle.ScreenshotBase64Async(
                    new ElementScreenshotOptions
                    {
                        Type = ScreenshotType.Png,
                        OmitBackground = true,
                    }
                );

                news.Base64Snapshot = contentElementSreenshot;
                news.Content = contentInnerText.RemoteObject.Value.ToString()
                    .Replace(news.Title, string.Empty)
                    .Replace($"{news.PublishDate:yyyy/MM/dd}", string.Empty)
                    .MarkDownEscape()
                    .Trim()
                    .TrimToDiscordEmbedLimited()
                    ;

                await page.CloseAsync();
            }
            catch (Exception) { }
        }

        public async Task<ConcurrentDictionary<string, WebPage>> GetAllLinkedWebPage(WebPage source, IBrowser browser, ConcurrentDictionary<string, WebPage> webPageDict, ConcurrentQueue<string> urlsQueue, bool isMainThread = true)
        {
            if (browser == null)
            {
                using BrowserFetcher browserFetcher = new();
                await browserFetcher.DownloadAsync();

                await using var newBrowser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = false,
                    DefaultViewport = null,
                    Args = [$"--window-size=450,450"],
                });

                return await GetAllLinkedWebPage(source, newBrowser, webPageDict, urlsQueue);
            }

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(source.Url, WaitUntilNavigation.Networkidle0);
            source.Name = await page.GetTitleAsync();

            Dictionary<string, string> contentRefs = new() {
                { "#main-article-new", string.Empty },
                { "#mainlong", string.Empty },
                { ".TbMainIE", string.Empty },
            };

            await Parallel.ForEachAsync(contentRefs, async (contentRef, cancellationToken) =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                try
                {
                    var targetQuery = contentRef.Key;
                    await page.WaitForSelectorAsync(targetQuery, new() { Timeout = 1 * 1000 });
                    IElementHandle queryElementHandle = await page.QuerySelectorAsync(targetQuery);
                    IJSHandle innerTextIJSHandle = await queryElementHandle.GetPropertyAsync("innerText");
                    contentRefs[targetQuery] = innerTextIJSHandle.RemoteObject.Value.ToString();
                }
                catch { }
            });

            foreach (var contentRef in contentRefs)
            {
                if (string.IsNullOrWhiteSpace(contentRef.Value)) continue;

                source.Snippet = contentRef.Value;
                break;
            }

            if (!webPageDict.ContainsKey(source.Url))
            {
                webPageDict.TryAdd(source.Url, source);
                SaveWebPage(source);
            }

            var jsSelectAllAnchors = @"Array.from(document.querySelectorAll('a')).map(a => a.href);";
            var urls = await page.EvaluateExpressionAsync<string[]>(jsSelectAllAnchors);
            await page.CloseAsync();
            string[] uniqueUrls =
            [
                "https://mabinogi.fws.tw/how_reform.php",
                "https://mabinogi.fws.tw/how_titles.php",
                "https://mabinogi.fws.tw/items.php",
                "https://mabinogi.fws.tw/ac_bbsfree_view.php",
                "https://mabinogi.fws.tw/ac_gallery_new_view.php",
                "https://mabinogi.fws.tw/ac_catwalk_view.php",
                "https://mabinogi.fws.tw/ac_bbpp.php",
                "https://mabinogi.fws.tw/ac_movies.php",
                "https://mabinogi.fws.tw/ac_photos_view.php",
                "https://mabinogi.fws.tw/go.php",
                "https://mabinogi.fws.tw/member_view.php",
                "https://mabinogi.fws.tw/how_enchant.php",
                "https://mabinogi.fws.tw/news_info.php",
                "https://mabinogi.fws.tw/weather.php",
            ];
            foreach (string url in urls)
            {
                if (string.IsNullOrEmpty(url)) continue;
                if (new Uri(source.Url).Host != new Uri(url).Host) continue;
                //if (!url.StartsWith("https://mabinogi.fws.tw/how")) continue;
                if (uniqueUrls.Any(x => url.StartsWith(x) && url != x))
                    continue;
                if (webPageDict.ContainsKey(url)) continue;

                urlsQueue.Enqueue(url);
            }
            if (!isMainThread) return webPageDict;

            await Parallel.ForAsync(0, 32, async (i, cancellationToken) =>
            {
            checkpoint:
                while (urlsQueue.TryDequeue(out string url))
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    WebPage newWebPage = new() { Url = url };
                    await GetAllLinkedWebPage(newWebPage, browser, webPageDict, urlsQueue, false);
                }
                Thread.Sleep(3000);
                if (!urlsQueue.IsEmpty) goto checkpoint;
            });

            return webPageDict;
        }

        public async void SaveWebPage(WebPage webPage)
        {
            string url = webPage.Url;
            string folderName = url.GetHashString(); ;
            string folderPath = Path.Combine("KernelMemory", "WebPage", folderName);
            DirectoryInfo directory = new(folderPath);
            if (!directory.Exists) directory.Create();

            FileInfo json = new(Path.Combine(folderPath, $"WebPage.json"));
            await File.AppendAllTextAsync(json.FullName, webPage.ToJsonString());

            if (string.IsNullOrWhiteSpace(webPage.Snippet)) return;

            FileInfo data = new FileInfo(Path.Combine(folderPath, $"{folderName}.txt"));
            await File.AppendAllTextAsync(data.FullName, webPage.Snippet);
        }
    }
}
