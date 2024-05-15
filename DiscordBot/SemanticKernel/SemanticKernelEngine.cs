using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive.Joins;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.Exporter;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Constant;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel.CustomClass;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.CustomSearchAPI.v1.Data;
using HandlebarsDotNet.Collections;
using Microsoft.Extensions.Configuration;
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
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using static DiscordBot.Helper.PromptHelper;

namespace DiscordBot.SemanticKernel
{
    public class SemanticKernelEngine(ILogger<SemanticKernelEngine> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, MabinogiKernelMemoryFactory mabiKMFactory, PromptHelper promptHelper)
    {
        public const string SystemPrompt = "你是一個Discord Bot, 名字叫夏夜小幫手, 你在\"夏夜月涼\"伺服器裡為會員們服務.";

        bool isEngineStarted = false;
        Microsoft.KernelMemory.AzureOpenAIConfig chatCompletionConfig;
        Microsoft.KernelMemory.AzureOpenAIConfig embeddingConfig;
        ApplicationInsightsConfig applicationInsightsConfig;

        public async Task StartEngine()
        {
            chatCompletionConfig = semanticKernelConfig.Value.AzureOpenAI.GPT4_Turbo_0409;
            embeddingConfig = semanticKernelConfig.Value.AzureOpenAI.Embedding;
            applicationInsightsConfig = semanticKernelConfig.Value.ApplicationInsightsConfig;

            using TracerProvider traceProvider = Sdk.CreateTracerProviderBuilder()
                //.AddSource("Microsoft.SemanticKernel*")
                .AddSource("SemanticKernel.Connectors.OpenAI")
                //.AddAzureMonitorTraceExporter(options => options.ConnectionString = applicationInsightsConfig.ConnectionString)
                .Build();

            using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                //.AddMeter("Microsoft.SemanticKernel*")
                .AddMeter("SemanticKernel.Connectors.OpenAI")
                //.AddAzureMonitorMetricExporter(options => options.ConnectionString = applicationInsightsConfig.ConnectionString)
                .Build();
        }

        public async Task<Kernel> GetKernelAsync(ICollection<LogRecord> logRecords = null)
        {
            if (!isEngineStarted) await StartEngine();

            // ... initialize the engine ...
            var builder = Kernel.CreateBuilder();
            builder
                .AddAzureOpenAIChatCompletion(
                    chatCompletionConfig.Deployment,
                    chatCompletionConfig.Endpoint,
                     chatCompletionConfig.APIKey)
                //.AddOpenAIChatCompletion(
                //    chatCompletionConfig.Deployment,
                //     chatCompletionConfig.APIKey,
                //     httpClient: new(new CustomHttpMessageHandler(chatCompletionConfig)))
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
                .AddFromType<Plugins.Math.MathPlugin>()
                .AddFromObject(new MabiMemoryPlugin(await mabiKMFactory.GetMabinogiKernelMemory(), waitForIngestionToComplete: true), "memory")
                .AddFromPromptDirectory("./SemanticKernel/Plugins/Writer")
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

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                var config = new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json")
                       .Build();
                IConfigurationSection section = config.GetSection(NLogConstant.SectionName);
                var loggingConfiguration = new LoggingConfiguration(new NLog.LogFactory());
                loggingConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new ConsoleTarget());
                loggingConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, new FileTarget
                {
                    FileName = section.GetValue<string>(NLogConstant.FileName),
                    Layout = section.GetValue<string>(NLogConstant.Layout),
                });

                loggingBuilder.AddNLog(config);
                loggingBuilder.AddOpenTelemetry(options =>
                {
                    //options
                    //.AddAzureMonitorLogExporter(options => options.ConnectionString = applicationInsightsConfig.ConnectionString)
                    //.AddConsoleExporter()
                    //;
                    if (logRecords != null) options.AddInMemoryExporter(logRecords);
                    // Format log messages. This is default to false.
                    options.IncludeFormattedMessage = true;
                });
            });

            Kernel kernel = builder.Build();

            kernel.FunctionInvoking += (sender, e) =>
            {
                logger.LogInformation($"{e.Function.Name} : Pre Function Execution Handler - Triggered");
            };
            kernel.FunctionInvoked += (sender, e) =>
            {
                logger.LogInformation($"{e.Function.Name} : Post Function Execution Handler");
            };
            kernel.PromptRendering += (sender, e) =>
            {
                logger.LogInformation($"{e.Function.Name} : Pre Prompt Render Handler - Triggered");
            };
            kernel.PromptRendered += (sender, e) =>
            {
                logger.LogInformation($"{e.Function.Name} : Post Prompt Render Handler");
            };
            return kernel;
        }

        public async Task<Conversation> GenerateResponse(string prompt)
        {
            DateTime startTime = DateTime.Now;
            ObservableCollection<LogRecord> logRecords = [];
            Kernel kernel = await GetKernelAsync(logRecords);

            ChatHistory history = [];
            history.AddSystemMessage(SystemPrompt);
            history.AddUserMessage(prompt);
            // 只有最後回答的用繁體中文回答, 風格: 可愛
            string additionalPromptContext = $"""
                背景: [{SystemPrompt}]
                你主要回答關於"瑪奇Mabinogi"的問題, 可以在long term memory裡找答案, 如果找不到(INFO NOT FOUND)就向用戶道歉
                """;
            var planner = new HandlebarsPlanner(new HandlebarsPlannerOptions()
            {
                // When using OpenAI models, we recommend using low values for temperature and top_p to minimize planner hallucinations.
                ExecutionSettings = new OpenAIPromptExecutionSettings()
                {
                    ChatSystemPrompt = SystemPrompt,
                    Temperature = 0.0,
                    TopP = 0.1,
                    MaxTokens = 4000,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                },
                // Use gpt-4 or newer models if you want to test with loops.
                // Older models like gpt-35-turbo are less recommended. They do handle loops but are more prone to syntax errors.
                AllowLoops = chatCompletionConfig.Deployment.Contains("gpt-4", StringComparison.OrdinalIgnoreCase),
                GetAdditionalPromptContext = async () => additionalPromptContext
            });
            HandlebarsPlan plan = await planner.CreatePlanAsync(kernel, prompt);
            string planTemplate = promptHelper.GetPlanTemplateFromPlan(plan);
            logger.LogInformation($"Plan steps: {Environment.NewLine}{planTemplate}");
            var planResult = (await plan.InvokeAsync(kernel)).Trim();

            history.AddUserMessage(planTemplate);
            history.AddAssistantMessage(planResult);

            Conversation conversation = new()
            {
                UserPrompt = prompt,
                PlanTemplate = planTemplate,
                Result = planResult,
                StartTime = startTime,
                EndTime = DateTime.Now,
                ChatHistory = history
            };
            conversation.SetTokens(logRecords);
            return conversation;
        }

        public async Task<Conversation> GenerateResponseFromStepwisePlanner(string prompt)
        {
            DateTime startTime = DateTime.Now;
            ObservableCollection<LogRecord> logRecords = [];
            Kernel kernel = await GetKernelAsync(logRecords);

            var config = new FunctionCallingStepwisePlannerOptions
            {
                MaxIterations = 5,
                MaxTokens = 8000,
                ExecutionSettings = new OpenAIPromptExecutionSettings()
                {
                    //ChatSystemPrompt = SystemPrompt,
                    Temperature = 0.0,
                    TopP = 0.1,
                    MaxTokens = 2000,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                },
            };

            var planner = new FunctionCallingStepwisePlanner(config);
            FunctionCallingStepwisePlannerResult result = await planner.ExecuteAsync(kernel, prompt);

            ChatHistory history = result.ChatHistory;
            StringBuilder sb1 = new();
            foreach (var record in history) sb1.AppendLine(record.ToString());

            Conversation conversation = new()
            {
                UserPrompt = prompt,
                PlanTemplate = sb1.ToString(),
                Result = result.FinalAnswer,
                StartTime = startTime,
                EndTime = DateTime.Now,
                ChatHistory = history
            };
            conversation.SetTokens(logRecords);
            return conversation;
        }

        public async Task<Conversation> GenerateResponseWithoutPlanner(string prompt)
        {
            DateTime startTime = DateTime.Now;
            ObservableCollection<LogRecord> logRecords = [];
            Kernel kernel = await GetKernelAsync(logRecords);

            ChatHistory history = [];
            history.AddSystemMessage(SystemPrompt);
            history.AddUserMessage(prompt);

            var memoryPrompt = @"
            Question to Kernel Memory: {{$input}}

            Kernel Memory Answer: {{memory.ask}}

            If the answer is empty say 'I don't know', otherwise reply with a business mail to share the answer.
            ";

            OpenAIPromptExecutionSettings settings = new()
            {
                ChatSystemPrompt = SystemPrompt,
                Temperature = 0.0,
                TopP = 0.1,
                MaxTokens = 4000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            KernelArguments arguments = new(settings)
            {
                { "input", prompt },
            };
            //+		ex	{"Missing argument for function parameter 'question'"}	System.Exception {Microsoft.SemanticKernel.KernelException}

            var response = await kernel.InvokePromptAsync(memoryPrompt, arguments);
            var result = response.GetValue<string>();

            history.AddAssistantMessage(result);

            var instructPrompt = $@"
           Question to Kernel Memory: {prompt}

           Kernel Memory Answer: {{memory.ask}}

           If the answer is empty say 'I don't know', otherwise reply with the answer.
           ";
            Conversation conversation = new()
            {
                UserPrompt = prompt,
                PlanTemplate = null,
                Result = result,
                StartTime = startTime,
                EndTime = DateTime.Now,
                ChatHistory = history
            };
            conversation.SetTokens(logRecords);
            return conversation;
        }

        public async Task<Kernel> GetKernelWithRelevantFunctions(string query)
        {
            Kernel kernel = await GetKernelAsync();

            // Create memory to store the functions
            var memoryStorage = new VolatileMemoryStore();
            var textEmbeddingGenerator = new AzureOpenAITextEmbeddingGenerationService(
                    embeddingConfig.Deployment,
                    embeddingConfig.Endpoint,
                    embeddingConfig.APIKey);
            var memory = new SemanticTextMemory(memoryStorage, textEmbeddingGenerator);

            // Save functions to memory
            foreach (KernelPlugin plugin in kernel.Plugins)
            {
                foreach (KernelFunction function in plugin)
                {
                    var fullyQualifiedName = $"{plugin.Name} - {function.Name}";
                    await memory.SaveInformationAsync(
                        "functions",
                        fullyQualifiedName + ": " + function.Description,
                        fullyQualifiedName,
                        additionalMetadata: function.Name
                        );
                }
            }

            // Retrieve the "relevant" functions
            var relevantRememberedFunctions = memory.SearchAsync("functions", query, 30, minRelevanceScore: 0.2);
            var relevantFoundFunctions = new List<KernelFunction>();
            // Populate a plugin with the filtered results
            await foreach (MemoryQueryResult relevantFunction in relevantRememberedFunctions)
            {
                foreach (KernelPlugin plugin in kernel.Plugins)
                {
                    if (plugin.TryGetFunction(relevantFunction.Metadata.AdditionalMetadata, out var function))
                    {
                        relevantFoundFunctions.Add(function);
                        break;
                    }
                }
            }
            KernelPlugin relevantFunctionsPlugin = KernelPluginFactory.CreateFromFunctions("Plugin", relevantFoundFunctions);

            var builder = Kernel.CreateBuilder();
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

            var kernelWithRelevantFunctions = builder.Build();
            kernelWithRelevantFunctions.Plugins.Add(relevantFunctionsPlugin);
            return kernelWithRelevantFunctions;
        }
    }
}
