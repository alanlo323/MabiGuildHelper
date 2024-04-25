using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Joins;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DocumentFormat.OpenXml.Spreadsheet;
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
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.Plugins.Document;
using Microsoft.SemanticKernel.Plugins.Document.FileSystem;
using Microsoft.SemanticKernel.Plugins.Document.OpenXml;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Google;
using static DiscordBot.Helper.PromptHelper;

namespace DiscordBot.SemanticKernel
{
    public class SemanticKernelEngine(ILogger<SemanticKernelEngine> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, MabinogiKernelMemoryFactory mabiKMFactory, PromptHelper promptHelper)
    {
        public const string SystemPrompt = "你是一個Discord Bot, 名字叫夏夜小幫手, 你在\"夏夜月涼\"伺服器裡為會員們服務.";

        Kernel kernel;
        AzureOpenAIConfig chatCompletionConfig;
        AzureOpenAIConfig embeddingConfig;

        public async Task StartEngine()
        {
            // ... initialize the engine ...
            var builder = Kernel.CreateBuilder();

            chatCompletionConfig = semanticKernelConfig.Value.AzureOpenAI.GPT4;
            embeddingConfig = semanticKernelConfig.Value.AzureOpenAI.Embedding;

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
                //.AddFromType<HttpPlugin>()
                //.AddFromType<TextPlugin>()
                //.AddFromType<WaitPlugin>()
                //.AddFromType<TimePlugin>()
                //.AddFromType<FileIOPlugin>()
                //.AddFromType<SearchUrlPlugin>()
                //.AddFromType<DocumentPlugin>()
                //.AddFromType<TextMemoryPlugin>()
                //.AddFromType<WebSearchEnginePlugin>()
                //.AddFromType<WebFileDownloadPlugin>()
                //.AddFromType<ConversationSummaryPlugin>()
                .AddFromType<Plugins.KernelMemory.Math.MathPlugin>()
                .AddFromObject(new MabiMemoryPlugin(await mabiKMFactory.GetMabinogiKernelMemory(), waitForIngestionToComplete: true), "memory")
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

            ChatHistory history = [];
            history.AddSystemMessage(SystemPrompt);
            history.AddUserMessage(prompt);

            string additionalPromptContext = $"""
                背景: [{SystemPrompt}]
                大部分的問題都是關於"瑪奇Mabinogi", 可以在long term memory裡找到答案.
                你盡量用中文回答
                風格: 輕鬆, 有趣, 有禮貌
                """;
            var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions()
            {
                // When using OpenAI models, we recommend using low values for temperature and top_p to minimize planner hallucinations.
                ExecutionSettings = new OpenAIPromptExecutionSettings()
                {
                    ChatSystemPrompt = SystemPrompt,
                    Temperature = 0.0,
                    TopP = 0.1,
                    MaxTokens = 4096,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                },
                // Use gpt-4 or newer models if you want to test with loops.
                // Older models like gpt-35-turbo are less recommended. They do handle loops but are more prone to syntax errors.
                AllowLoops = chatCompletionConfig.Deployment.Contains("gpt-4", StringComparison.OrdinalIgnoreCase),
                GetAdditionalPromptContext = async () => additionalPromptContext
            });
            HandlebarsPlan plan = await planner.CreatePlanAsync(kernel, prompt);
            List<PlanStep> planSteps = promptHelper.GetStepFromPlan(plan);
            var planResult = (await plan.InvokeAsync(kernel)).Trim();

            #region backup
            //// Get the response from the AI
            //var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            //var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
            //                    history,
            //                    executionSettings: openAIPromptExecutionSetting,
            //                    kernel: kernel);

            //string fullMessage = "";
            //var first = true;
            //await foreach (var content in result)
            //{
            //    if (content.Role.HasValue && first)
            //    {
            //        first = false;
            //    }
            //    fullMessage += content.Content;
            //}
            //history.AddAssistantMessage(fullMessage);
            #endregion

            foreach (var planStep in planSteps)
            {
                history.AddAssistantMessage($"{planStep.FullDisplayName}".ToHighLight());
                foreach (var actionRow in planStep.ActionRows) history.AddAssistantMessage($"{actionRow}");
            }
            StringBuilder sb1 = new(), sb2 = new();
            foreach (var record in history) if (record.Role == AuthorRole.Assistant) sb1.AppendLine(record.Items.OfType<TextContent>().FirstOrDefault()?.Text);
            sb2.Append(sb1.ToString().ToHidden());
            sb2.Append(planResult.ToQuotation());
            history.AddAssistantMessage(planResult.ToQuotation());

            return sb2.ToString();
        }
    }
}
