using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reactive.Joins;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.Exporter;
using Discord.WebSocket;
using DiscordBot.Commands.SlashCommand;
using DiscordBot.Configuration;
using DiscordBot.Constant;
using DiscordBot.Db;
using DiscordBot.Db.Entity;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SemanticKernel.Core;
using DiscordBot.SemanticKernel.Plugins.KernelMemory;
using DiscordBot.SemanticKernel.Plugins.Web;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.CustomSearchAPI.v1.Data;
using HandlebarsDotNet.Collections;
using Microsoft.Extensions.Azure;
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
using Elastic.Clients.Elasticsearch;
using DiscordBot.SemanticKernel.Plugins.About;
using Azure.Core;

namespace DiscordBot.SemanticKernel
{
    public class SemanticKernelEngine(ILogger<SemanticKernelEngine> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, MabinogiKernelMemoryFactory mabiKMFactory, PromptHelper promptHelper, AppDbContext appDbContext)
    {
        public const string SystemPrompt = "你是一個Discord Bot, 名字叫夏夜小幫手, 你在\"夏夜月涼\"伺服器裡為會員們服務.";

        bool isEngineStarted = false;
        AzureOpenAIConfig chatCompletionConfig;
        AzureOpenAIConfig embeddingConfig;
        ApplicationInsightsConfig applicationInsightsConfig;

        public async Task StartEngine()
        {
            chatCompletionConfig = semanticKernelConfig.Value.AzureOpenAI.GPT4O;
            embeddingConfig = semanticKernelConfig.Value.AzureOpenAI.Embedding;
            applicationInsightsConfig = semanticKernelConfig.Value.ApplicationInsightsConfig;

            using TracerProvider traceProvider = Sdk.CreateTracerProviderBuilder()
                //.AddSource("Microsoft.SemanticKernel*")
                .AddSource("SemanticKernel.Connectors.OpenAI")
                .AddSource("Microsoft.SemanticKernel*")
                //.AddAzureMonitorTraceExporter(options => options.ConnectionString = applicationInsightsConfig.ConnectionString)
                .Build();

            using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                //.AddMeter("Microsoft.SemanticKernel*")
                .AddMeter("SemanticKernel.Connectors.OpenAI")
                .AddMeter("Microsoft.SemanticKernel*")
                //.AddAzureMonitorMetricExporter(options => options.ConnectionString = applicationInsightsConfig.ConnectionString)
                .Build();

            isEngineStarted = true;
        }

        public async Task<Kernel> GetKernelAsync(ICollection<LogRecord> logRecords = null, AutoFunctionInvocationFilter autoFunctionInvocationFilter = null)
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
                .AddFromType<TimePlugin>()
                //.AddFromType<FileIOPlugin>()
                .AddFromType<WebSearchPlugin>()
                //.AddFromType<SearchUrlPlugin>()
                //.AddFromType<DocumentPlugin>()
                //.AddFromType<TextMemoryPlugin>()
                .AddFromType<Plugins.Math.MathPlugin>()
                //.AddFromType<WebFileDownloadPlugin>()
                //.AddFromType<Plugins.About.AboutPlugin>()
                .AddFromType<Plugins.Writer.Summary.ConversationSummaryPlugin>()
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
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                });
            });

            builder.Services.AddSingleton(appDbContext);
            builder.Services.AddScoped<DatabaseHelper>();
            builder.Services.AddScoped<DataScrapingHelper>();

            if (autoFunctionInvocationFilter != default)
            {
                builder.Services.AddSingleton<IAutoFunctionInvocationFilter>(autoFunctionInvocationFilter);
                builder.Services.AddSingleton<IFunctionInvocationFilter>(autoFunctionInvocationFilter);
            }

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

        public async Task<KernelStatus> GenerateResponse(string prompt, SocketSlashCommand command, bool showStatusPerSec = false, EventHandler<KernelStatus> onKenelStatusUpdatedCallback = null) => await GenerateResponseWithChatCompletionService(prompt, command, showStatusPerSec: showStatusPerSec, onKenelStatusUpdatedCallback: onKenelStatusUpdatedCallback);

        public async Task<KernelStatus> GenerateResponseFromHandlebarsPlanner(string prompt, SocketSlashCommand command, EventHandler<KernelStatus> onKenelStatusUpdatedCallback, bool showStatusPerSec = false)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                KernelStatus kernelStatus = new();
                ObservableCollection<LogRecord> logRecords = [];
                AutoFunctionInvocationFilter autoFunctionInvocationFilter = new(kernelStatus, onKenelStatusUpdatedCallback, showStatusPerSec: showStatusPerSec);
                Kernel kernel = await GetKernelAsync(logRecords: logRecords, autoFunctionInvocationFilter: autoFunctionInvocationFilter);

                ChatHistory history = [];
                history.AddSystemMessage(SystemPrompt);
                history.AddUserMessage(prompt);

                StepStatus planStatus = new()
                {
                    DisplayName = "CreatePlan",
                    Status = StatusEnum.Running,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec
                };
                kernelStatus.StepStatuses.Enqueue(planStatus);

                string additionalPromptContext = $"""
                {SystemPrompt}
                你主要回答關於"瑪奇Mabinogi"的問題, 可以在long term memory裡找答案, 如果找不到(INFO NOT FOUND)就向用戶道歉
                最後使用繁體中文來回覆
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

                using System.Timers.Timer statusReportTimer = new(1000) { AutoReset = true };
                statusReportTimer.Elapsed += (sender, e) => { onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus); };
                if (showStatusPerSec) statusReportTimer.Start();

                HandlebarsPlan plan = await planner.CreatePlanAsync(kernel, prompt, arguments: new()
                {
                    { "username", command.User.Username }
                });
                string planTemplate = promptHelper.GetPlanTemplateFromPlan(plan);
                logger.LogInformation($"Plan steps: {Environment.NewLine}{planTemplate}");

                planStatus.Status = StatusEnum.Completed;
                planStatus.EndTime = DateTime.Now;

                var planResult = (await plan.InvokeAsync(kernel)).Trim();
                if (showStatusPerSec) statusReportTimer.Stop();

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
                kernelStatus.Conversation = conversation;

                return kernelStatus;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<KernelStatus> GenerateResponseFromStepwisePlanner(string prompt, SocketSlashCommand command, EventHandler<KernelStatus> onKenelStatusUpdatedCallback, bool showStatusPerSec = false)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                KernelStatus kernelStatus = new();
                ObservableCollection<LogRecord> logRecords = [];
                AutoFunctionInvocationFilter autoFunctionInvocationFilter = new(kernelStatus, onKenelStatusUpdatedCallback, showStatusPerSec: showStatusPerSec);
                Kernel kernel = await GetKernelAsync(logRecords: logRecords, autoFunctionInvocationFilter: autoFunctionInvocationFilter);

                ChatHistory history = [];

                StepStatus planStatus = new()
                {
                    DisplayName = "Thinking",
                    Status = StatusEnum.Thinking,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec
                };
                kernelStatus.StepStatuses.Enqueue(planStatus);

                string additionalPromptContext = $"""
                {SystemPrompt}
                你主要回答關於"瑪奇Mabinogi"的問題, 可以在long term memory裡找答案, 如果找不到(INFO NOT FOUND)就向用戶道歉
                最後使用繁體中文來回覆
                """;
                var config = new FunctionCallingStepwisePlannerOptions
                {
                    MaxIterations = 5,
                    MaxTokens = 8000,
                    ExecutionSettings = new OpenAIPromptExecutionSettings()
                    {
                        //ChatSystemPrompt = additionalPromptContext,
                        Temperature = 0.0,
                        TopP = 0.1,
                        MaxTokens = 4000,
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    },
                };

                var planner = new FunctionCallingStepwisePlanner(config);

                using System.Timers.Timer statusReportTimer = new(1000) { AutoReset = true };
                statusReportTimer.Elapsed += (sender, e) =>
                {
                    onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus);
                };
                if (showStatusPerSec) statusReportTimer.Start();
                FunctionCallingStepwisePlannerResult result = await planner.ExecuteAsync(kernel, prompt, chatHistoryForSteps: history);
                if (showStatusPerSec) statusReportTimer.Stop();

                history = result.ChatHistory;
                StringBuilder sb1 = new();
                foreach (var record in history!) sb1.AppendLine(record.ToString());

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
                kernelStatus.Conversation = conversation;
                kernelStatus.StepStatuses = new(kernelStatus.StepStatuses.Where(x => x.Status != StatusEnum.Thinking));

                return kernelStatus;
            }
            catch (Exception)
            {
                throw;
            }
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

        public async Task<KernelStatus> GenerateResponseWithChatCompletionService(string prompt, SocketSlashCommand command, EventHandler<KernelStatus> onKenelStatusUpdatedCallback, bool showStatusPerSec = false)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                KernelStatus kernelStatus = new();
                ObservableCollection<LogRecord> logRecords = [];
                AutoFunctionInvocationFilter autoFunctionInvocationFilter = new(kernelStatus, onKenelStatusUpdatedCallback, showStatusPerSec: showStatusPerSec);
                Kernel kernel = await GetKernelAsync(logRecords: logRecords, autoFunctionInvocationFilter: autoFunctionInvocationFilter);
                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

                string additionalPromptContext = $"""
                {SystemPrompt}
                先使用memory plugin在long term memory裡嘗試尋找答案, 如果找不到(INFO NOT FOUND)才用其他方法 (在memory裡找到的資料需要附上來源和可信度[XX%])
                使用繁體中文來回覆
                """;

                ChatHistory history = [];
                history.AddSystemMessage(additionalPromptContext);
                history.AddUserMessage(prompt);

                StepStatus planStatus = new()
                {
                    DisplayName = "Thinking",
                    Status = StatusEnum.Thinking,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = showStatusPerSec
                };
                kernelStatus.StepStatuses.Enqueue(planStatus);
                using System.Timers.Timer statusReportTimer = new(1000) { AutoReset = true };
                statusReportTimer.Elapsed += (sender, e) => { onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus); };

                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    Temperature = 0.0,
                    TopP = 0.1,
                    MaxTokens = 4000,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                };

                if (showStatusPerSec) statusReportTimer.Start();
                var content = await chatCompletionService.GetChatMessageContentAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: kernel);
                if (showStatusPerSec) statusReportTimer.Stop();

                StringBuilder sb1 = new();
                foreach (var record in history!.Where(x => x.Role != AuthorRole.System)) sb1.AppendLine(record.ToString());

                Conversation conversation = new()
                {
                    UserPrompt = prompt,
                    PlanTemplate = sb1.ToString(),
                    Result = $"{content}",
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    ChatHistory = history
                };
                conversation.SetTokens(logRecords);
                kernelStatus.Conversation = conversation;
                kernelStatus.StepStatuses = new(kernelStatus.StepStatuses.Where(x => x.Status != StatusEnum.Thinking));

                return kernelStatus;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
