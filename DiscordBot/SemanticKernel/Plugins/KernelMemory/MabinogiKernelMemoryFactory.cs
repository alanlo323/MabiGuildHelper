using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.Core;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage.DevTools;
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

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory
{
    public class MabinogiKernelMemoryFactory(ILogger<MabinogiKernelMemoryFactory> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, DataScrapingHelper dataScrapingHelper)
    {
        IKernelMemory? memory;

        public async Task<IKernelMemory> GetMabinogiKernelMemory()
        {
            if (memory != null) return memory;
            try
            {

                var kernelMemoryBuilder = new KernelMemoryBuilder();
                memory = kernelMemoryBuilder
                    .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
                    .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
                    .WithSearchClientConfig(new() { MaxMatchesCount = 3, AnswerTokens = 2000 })
                    .WithAzureOpenAITextGeneration(semanticKernelConfig.Value.AzureOpenAI.GPT4_Turbo_0409)
                    .WithAzureOpenAITextEmbeddingGeneration(semanticKernelConfig.Value.AzureOpenAI.Embedding)
                    //.WithCustomTextGenerator(new CustomModelTextGeneration(ollama, new() { MaxToken = 8 * 1024 }))
                    //.WithCustomEmbeddingGenerator(new CustomEmbeddingGenerator(ollama, new() { MaxToken = 8 * 1024, TokenEncodingName = kernelMemoryConfig.Value.TokenEncodingName }))
                    .WithCustomPromptProvider(new CustomPromptProvider())
                    .WithCustomTextPartitioningOptions(new TextPartitioningOptions
                    {
                        MaxTokensPerParagraph = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal,
                        MaxTokensPerLine = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal / 10
                    })
                    .Build();

            }
            catch (Exception)
            {

                throw;
            }
            await ImportData();

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
                    await memory.ImportDocumentAsync(data.FullName, documentId: subfolder.Name, tags: new TagCollection() { webPage.Name });
                    continue;
                }

                // Remove URL fragment if it exists
                UriBuilder uriBuilder = new(webPage.Url)
                {
                    Fragment = string.Empty
                };
                string cleanUrl = uriBuilder.Uri.ToString();
                if (cleanUrl.EndsWith(".jpg")) continue;
                await memory.ImportWebPageAsync(cleanUrl, documentId: subfolder.Name, tags: new TagCollection() { webPage.Name });

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
