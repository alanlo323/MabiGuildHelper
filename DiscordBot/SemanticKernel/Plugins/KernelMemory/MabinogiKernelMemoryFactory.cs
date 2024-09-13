using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using StackExchange.Redis;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Connectors.Redis;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;
using DiscordBot.Constant;
using NLog.Config;
using NLog.Targets;
using OpenTelemetry.Logs;
using NLog.Extensions.Logging;
using DiscordBot.Extension;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory
{
    public class MabinogiKernelMemoryFactory(ILogger<MabinogiKernelMemoryFactory> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, IOptionsSnapshot<RabbitMqConfig> rabbitMqConfig, IOptionsSnapshot<QdrantConfig> qdrantConfig, IOptionsSnapshot<ConnectionStringsConfig> connectionStringsConfig, IOptionsSnapshot<AzureBlobsConfig> azureBlobsConfig, DataScrapingHelper dataScrapingHelper, AppDbContext appDbContext)
    {
        IKernelMemory? memory;

        public async Task Prepare()
        {
            if (memory != null) return;
            try
            {
                var tags = new Dictionary<string, char?>
                {
                    { "__part_n", '|' },
                    { "SourceType", '|' },
                    { "Source", '|' },
                    { "Url", '|' },
                    { "Name", '|' },
                };
                var hostBuilder = Host.CreateApplicationBuilder();
                hostBuilder.Services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                    var config = new ConfigurationBuilder()
                           .AddJsonFile("appsettings.json")
                           .Build();
                    IConfigurationSection section = config.GetSection(NLogConstant.SectionName);
                    var loggingConfiguration = new LoggingConfiguration(new NLog.LogFactory());
                    loggingConfiguration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ConsoleTarget());
                    loggingConfiguration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new FileTarget
                    {
                        FileName = section.GetValue<string>(NLogConstant.FileName),
                        Layout = section.GetValue<string>(NLogConstant.Layout),
                    });

                    loggingBuilder.AddNLog(loggingConfiguration);
                });

                KernelMemoryBuilder kernelMemoryBuilder = new(hostBuilder.Services);
                kernelMemoryBuilder
                     .WithRabbitMQOrchestration(rabbitMqConfig.Value)
                     .WithRedisMemoryDb(new RedisConfig(tags: tags) { ConnectionString = connectionStringsConfig.Value.Redis })
                     .WithAzureBlobsDocumentStorage(azureBlobsConfig.Value)
                     .WithSearchClientConfig(new() { MaxMatchesCount = 1, AnswerTokens = 4000 })
                     .WithAzureOpenAITextGeneration(semanticKernelConfig.Value.AzureOpenAI.GPT4oMini, textTokenizer: new GPT4oTokenizer())
                     .WithAzureOpenAITextEmbeddingGeneration(semanticKernelConfig.Value.AzureOpenAI.Embedding, textTokenizer: new GPT3Tokenizer())
                     .WithCustomTextPartitioningOptions(new TextPartitioningOptions
                     {
                         MaxTokensPerParagraph = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal,
                         MaxTokensPerLine = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal / 10
                     })
                     ;

                kernelMemoryBuilder.Services.AddSingleton(this);
                kernelMemoryBuilder.Services.AddSingleton(semanticKernelConfig);
                kernelMemoryBuilder.Services.AddSingleton(appDbContext);
                kernelMemoryBuilder.Services.AddSingleton<DiscordMessageHandler>();

                memory = kernelMemoryBuilder.Build();
                if (memory is MemoryServerless memoryServerless)
                {
                    memoryServerless.Orchestrator.AddHandler<DiscordMessageHandler>(semanticKernelConfig.Value.KernelMemory.Discord.Steps[0]);
                }
                if (memory is MemoryService memoryService)
                {
                    IPipelineOrchestrator orchestrator = memoryService.GetField<IPipelineOrchestrator>("_orchestrator");
                    IServiceProvider serviceProvider = hostBuilder.Services.BuildServiceProvider();
                    DiscordMessageHandler discordMessageHandler = ActivatorUtilities.CreateInstance<DiscordMessageHandler>(serviceProvider, semanticKernelConfig.Value.KernelMemory.Discord.Steps[0]);
                    await orchestrator.AddHandlerAsync(discordMessageHandler);
                }

                await ImportData();

                var host = hostBuilder.Build();
                await host.StartAsync();
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
            //await ImportWebData();
        }

        // Modify the ImportWebData method in the MabinogiKernelMemoryFactory class

        private async Task ImportWebData()
        {
            ConcurrentDictionary<string, WebPage> webPageDict = new();
            string folderPath = Path.Combine("KernelMemory", "WebPage");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
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
                if (cleanUrl.EndsWith(".png")) continue;
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
                if (documentIsReady) continue;

                TagCollection tags = new()
                    {
                        { "SourceType", "File" },
                        { "Name", fileInfo.Name},
                    };
                await memory.ImportDocumentAsync(fileInfo.FullName, documentId: fileInfo.Name, tags: tags);
            }
        }
    }
}
