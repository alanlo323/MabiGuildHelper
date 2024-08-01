using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.Core;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Prompts;
using Microsoft.KernelMemory;
using OllamaSharp;
using DiscordBot.Commands.SlashCommand;
using Microsoft.Extensions.Logging;
using DiscordBot.Configuration;
using Microsoft.Extensions.Options;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.AI.OpenAI;
using DocumentFormat.OpenXml.Bibliography;
using DiscordBot.Helper;
using Newtonsoft.Json;
using Microsoft.SemanticKernel.Plugins.Web;
using System.Collections.Concurrent;
using System.Security.Policy;
using DiscordBot.Util;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using DiscordBot.Db;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.KernelMemory.DocumentStorage.DevTools;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory
{
    public class MabinogiKernelMemoryFactory(ILogger<MabinogiKernelMemoryFactory> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, IOptionsSnapshot<ConnectionStringsConfig> connectionStringsConfig, DataScrapingHelper dataScrapingHelper, AppDbContext appDbContext)
    {
        IKernelMemory? memory;
        public async Task Prepare()
        {
            if (memory != null) return;
            try
            {
                // TODO: Use reak db and storage
                KernelMemoryBuilder kernelMemoryBuilder = new();
                kernelMemoryBuilder
                     .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
                     .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
                     .WithSearchClientConfig(new() { MaxMatchesCount = 1, AnswerTokens = 2000 })
                     .WithAzureOpenAITextGeneration(semanticKernelConfig.Value.AzureOpenAI.GPT4oMini, textTokenizer: new GPT4oTokenizer())
                     .WithAzureOpenAITextEmbeddingGeneration(semanticKernelConfig.Value.AzureOpenAI.Embedding, textTokenizer: new GPT4oTokenizer())
                     //.WithCustomTextGenerator(new CustomModelTextGeneration(ollama, new() { MaxToken = 8 * 1024 }))
                     //.WithCustomEmbeddingGenerator(new CustomEmbeddingGenerator(ollama, new() { MaxToken = 8 * 1024, TokenEncodingName = kernelMemoryConfig.Value.TokenEncodingName }))
                     .WithCustomPromptProvider(new CustomPromptProvider())
                     .WithCustomTextPartitioningOptions(new TextPartitioningOptions
                     {
                         MaxTokensPerParagraph = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal,
                         MaxTokensPerLine = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal / 10
                     })
                     ;

                kernelMemoryBuilder.Services.AddSingleton(this);
                kernelMemoryBuilder.Services.AddSingleton(semanticKernelConfig);
                kernelMemoryBuilder.Services.AddSingleton(appDbContext);

                memory = kernelMemoryBuilder.Build();
                (memory as MemoryServerless)!.Orchestrator.AddHandler<DiscordMessageHandler>(semanticKernelConfig.Value.KernelMemory.Discord.Steps[0]);

                await ImportData();
            }
            catch
            {
                throw;
            }
        }

        public async Task<IKernelMemory> GetMabinogiKernelMemory()
        {
            await Prepare();
            return memory;
        }

        private async Task ImportData()
        {
            //await ImportAppData();
            await ImportWebData();
        }

        // Modify the ImportWebData method in the MabinogiKernelMemoryFactory class

        private async Task ImportWebData()
        {
            ConcurrentDictionary<string, WebPage> webPageDict = new();
            string folderPath = Path.Combine("KernelMemory", "WebPage");
            DirectoryInfo preloadedFolder = new(folderPath);
            DirectoryInfo[] subfolders = preloadedFolder.GetDirectories();
            foreach (var subfolder in subfolders)
            {
                FileInfo json = new(Path.Combine(subfolder.FullName, $"WebPage.json"));
                WebPage? webPage = JsonConvert.DeserializeObject<WebPage>(await File.ReadAllTextAsync(json.FullName));
                webPageDict.TryAdd(webPage.Url, webPage);
            }

            Website[] websites = semanticKernelConfig.Value.KernelMemory.DataSource.Website;
            foreach (var website in websites)
            {
                //await dataScrapingHelper.GetAllLinkedWebPage(new() { Url = website.Url, Name = website.Name }, null, webPageDict, []);
            }

            subfolders = preloadedFolder.GetDirectories();
            foreach ((DirectoryInfo subfolder, int index) in subfolders.Select((subfolder, index) => (subfolder, index)))
            {
                if (await memory.IsDocumentReadyAsync(subfolder.Name)) continue;

                FileInfo json = new(Path.Combine(subfolder.FullName, $"WebPage.json"));
                WebPage? webPage = JsonConvert.DeserializeObject<WebPage>(await File.ReadAllTextAsync(json.FullName));
                bool isValidUrl = Uri.TryCreate(webPage.Url, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) && !webPage.Url.EndsWith("#");
                if (!isValidUrl) continue;

                FileInfo data = new(Path.Combine(subfolder.FullName, $"{subfolder.Name}.txt"));
                if (false && data.Exists)
                {
                    TagCollection docTags = new()
                    {
                        { "SourceType", "WebPage" },
                        { "Source", webPage.Url },
                        { "Url", webPage.Url },
                        { "Name", webPage.Name},
                    };
                    await memory.ImportDocumentAsync(data.FullName, documentId: subfolder.Name, tags: docTags);
                    continue;
                }

                // Remove URL fragment if it exists
                UriBuilder uriBuilder = new(webPage.Url)
                {
                    Fragment = string.Empty
                };
                string cleanUrl = uriBuilder.Uri.ToString();
                if (cleanUrl.EndsWith(".jpg")) continue;
                TagCollection tags = new()
                    {
                        { "SourceType", "WebPage" },
                        { "Source", webPage.Url },
                        { "Url", webPage.Url },
                        { "Name", webPage.Name},
                    };
                await memory.ImportWebPageAsync(cleanUrl, documentId: subfolder.Name, tags: tags);

                logger.LogInformation($"Imported {index + 1}/{subfolders.Length} WebPage: {webPage.Name} Url: {webPage.Url}");
            }
        }

        private async Task ImportAppData()
        {
            foreach (var path in Directory.GetFiles(Path.Combine("AppData", "KMData")))
            {
                FileInfo fileInfo = new(path);

                var documentIsReady = await memory.IsDocumentReadyAsync(fileInfo.Name);
                if (documentIsReady)
                {
                    continue;
                }
                //await memory.ImportDocumentAsync(fileInfo.FullName, documentId: fileInfo.Name, tags: new TagCollection() { fileInfo.Name });
            }
        }
    }
}
