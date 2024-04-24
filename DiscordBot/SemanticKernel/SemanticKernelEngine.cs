using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.Document;
using Microsoft.SemanticKernel.Plugins.Document.FileSystem;
using Microsoft.SemanticKernel.Plugins.Document.OpenXml;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Google;

namespace DiscordBot.SemanticKernel
{
    public class SemanticKernelEngine(ILogger<SemanticKernelEngine> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, MabinogiKernelMemoryFactory mabiKMFactory)
    {
        public const string SystemPrompt = "你是一個Discord Bot, 名字叫夏夜小幫手, 你在\"夏夜月涼\"伺服器裡為會員們服務.";

        Kernel kernel;

        public async Task StartEngine()
        {
            // ... initialize the engine ...
            var builder = Kernel.CreateBuilder();

            AzureOpenAIConfig chatCompletionConfig = semanticKernelConfig.Value.AzureOpenAI.GPT35;
            AzureOpenAIConfig embeddingConfig = semanticKernelConfig.Value.AzureOpenAI.Embedding;
            builder
                .AddAzureOpenAIChatCompletion(
                    chatCompletionConfig.Deployment,
                    chatCompletionConfig.Endpoint,
                     chatCompletionConfig.APIKey)
                .AddAzureOpenAITextEmbeddingGeneration(
                    embeddingConfig.Deployment,
                    embeddingConfig.Endpoint,
                    embeddingConfig.APIKey)
                ;

            builder.Plugins
                .AddFromType<HttpPlugin>()
                .AddFromType<TextPlugin>()
                .AddFromType<WaitPlugin>()
                .AddFromType<TimePlugin>()
                .AddFromType<MathPlugin>()
                .AddFromType<FileIOPlugin>()
                .AddFromType<SearchUrlPlugin>()
                .AddFromType<DocumentPlugin>()
                .AddFromType<TextMemoryPlugin>()
                .AddFromType<WebSearchEnginePlugin>()
                .AddFromType<WebFileDownloadPlugin>()
                .AddFromType<ConversationSummaryPlugin>()
                .AddFromObject(new MemoryPlugin(await mabiKMFactory.GetMabinogiKernelMemory(), waitForIngestionToComplete: true), "memory")
                ;

            builder.Services
                .AddScoped<IDocumentConnector, WordDocumentConnector>()
                .AddScoped<IFileSystemConnector, LocalFileSystemConnector>()
                .AddScoped<ISemanticTextMemory, SemanticTextMemory>()
                .AddScoped<IMemoryStore, VolatileMemoryStore>()
                .AddScoped<IWebSearchEngineConnector, GoogleConnector>(x =>
                {
                    return new GoogleConnector(
                        semanticKernelConfig.Value.GoogleSearchApi.ApiKey,
                        semanticKernelConfig.Value.GoogleSearchApi.SearchEngineId
                        );
                })
                ;

            kernel = builder.Build();
        }

        public async Task<string> GenerateResponse(string prompt)
        {
            if (kernel == null) await StartEngine();
            OpenAIPromptExecutionSettings openAIPromptExecutionSetting = new()
            {
                MaxTokens = 100,
                //ChatSystemPrompt = SystemPrompt,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
            //var basicKernelFunction = kernel.CreateFunctionFromPrompt(prompt, executionSettings: openAIPromptExecutionSetting);

            ChatHistory history = [];
            history.AddSystemMessage(SystemPrompt);
            history.AddUserMessage(prompt);
            // Get the response from the AI
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
                                history,
                                executionSettings: openAIPromptExecutionSetting,
                                kernel: kernel);

            string fullMessage = "";
            var first = true;
            await foreach (var content in result)
            {
                if (content.Role.HasValue && first)
                {
                    first = false;
                }
                fullMessage += content.Content;
            }
            history.AddAssistantMessage(fullMessage);

            return fullMessage;
        }
    }
}
