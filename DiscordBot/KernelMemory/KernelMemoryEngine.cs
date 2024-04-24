using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.KernelMemory.Core;
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

namespace DiscordBot.KernelMemory
{
    public class KernelMemoryEngine(ILogger<LuckyChannelCommand> logger, IOptionsSnapshot<Configuration.KernelMemoryConfig> kernelMemoryConfig)
    {
        bool isReady = false;
        IKernelMemory memory;

        public async Task StartEngine()
        {
            if (memory != null)
            {
                logger.LogWarning("KernelMemoryEngine already started");
                return;
            }

            OllamaApiClient ollama = new(new Uri(kernelMemoryConfig.Value.Ollama.Url))
            {
                SelectedModel = kernelMemoryConfig.Value.Ollama.Model
            };

            var azureOpenAITextConfig = new AzureOpenAIConfig();
            var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build()
                .BindSection("KernelMemory:Services:AzureOpenAIText", azureOpenAITextConfig)
                .BindSection("KernelMemory:Services:AzureOpenAIEmbedding", azureOpenAIEmbeddingConfig)
                ;

            var kernelMemoryBuilder = new KernelMemoryBuilder();
            memory = kernelMemoryBuilder
                   .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
                   .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
                    .WithSearchClientConfig(new() { MaxMatchesCount = 3, AnswerTokens = 1000 })
                    .WithAzureOpenAITextGeneration(azureOpenAITextConfig, new DefaultGPTTokenizer())
                    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig, new DefaultGPTTokenizer())
                   //.WithCustomTextGenerator(new CustomModelTextGeneration(ollama, new() { MaxToken = 8 * 1024 }))
                   //.WithCustomEmbeddingGenerator(new CustomEmbeddingGenerator(ollama, new() { MaxToken = 8 * 1024, TokenEncodingName = kernelMemoryConfig.Value.TokenEncodingName }))
                   .WithCustomPromptProvider(new CustomPromptProvider())
                    .WithCustomTextPartitioningOptions(new TextPartitioningOptions
                    {
                        MaxTokensPerParagraph = azureOpenAIEmbeddingConfig.MaxTokenTotal,
                        MaxTokensPerLine = azureOpenAIEmbeddingConfig.MaxTokenTotal / 10
                    })
                   .Build()
               ;
            await ImportData();
            isReady = true;
        }

        private async Task ImportData()
        {
            //await ImportAppData();
            await ImportWebData();
        }

        private async Task ImportWebData()
        {
            Website[] websites = kernelMemoryConfig.Value.DataSource.Website;
            foreach (var website in websites)
            {
                var documentIsReady = await memory.IsDocumentReadyAsync(website.Name);
                if (documentIsReady)
                {
                    continue;
                }
                await memory.ImportWebPageAsync(website.Url, documentId: website.Name, tags: new TagCollection() { website.Name });
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
                await memory.ImportDocumentAsync(fileInfo.FullName, documentId: fileInfo.Name, tags: new TagCollection() { fileInfo.Name });
            }
        }

        public async Task<MemoryAnswer> AskAsync(string question)
        {
            while (!isReady) Thread.Sleep(1000);
            MemoryAnswer answer = await memory.AskAsync(question);
            return answer;
        }
    }
}
