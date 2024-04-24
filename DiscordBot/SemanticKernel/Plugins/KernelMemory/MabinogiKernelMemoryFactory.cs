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

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory
{
    public class MabinogiKernelMemoryFactory(ILogger<MabinogiKernelMemoryFactory> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig)
    {
        IKernelMemory? memory;

        public async Task<IKernelMemory> GetMabinogiKernelMemory()
        {
            if (memory != null) return memory;

            var kernelMemoryBuilder = new KernelMemoryBuilder();
            memory = kernelMemoryBuilder
                .WithSimpleVectorDb(SimpleVectorDbConfig.Persistent)
                .WithSimpleFileStorage(SimpleFileStorageConfig.Persistent)
                .WithSearchClientConfig(new() { MaxMatchesCount = 3, AnswerTokens = 1000 })
                .WithAzureOpenAITextGeneration(semanticKernelConfig.Value.AzureOpenAI.GPT35, new DefaultGPTTokenizer())
                .WithAzureOpenAITextEmbeddingGeneration(semanticKernelConfig.Value.AzureOpenAI.Embedding, new DefaultGPTTokenizer())
                //.WithCustomTextGenerator(new CustomModelTextGeneration(ollama, new() { MaxToken = 8 * 1024 }))
                //.WithCustomEmbeddingGenerator(new CustomEmbeddingGenerator(ollama, new() { MaxToken = 8 * 1024, TokenEncodingName = kernelMemoryConfig.Value.TokenEncodingName }))
                .WithCustomPromptProvider(new CustomPromptProvider())
                .WithCustomTextPartitioningOptions(new TextPartitioningOptions
                {
                    MaxTokensPerParagraph = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal,
                    MaxTokensPerLine = semanticKernelConfig.Value.AzureOpenAI.Embedding.MaxTokenTotal / 10
                })
                .Build();

            await ImportData();

            return memory;
        }

        private async Task ImportData()
        {
            //await ImportAppData();
            await ImportWebData();
        }

        private async Task ImportWebData()
        {
            Website[] websites = semanticKernelConfig.Value.KernelMemory.DataSource.Website;
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
    }
}
