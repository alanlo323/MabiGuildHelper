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
using Discord;
using DiscordBot.Util;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.CodeInterpretion;
using SemanticKernel.Assistants.AutoGen.Plugins;
using CodeInterpretionPlugin = DiscordBot.SemanticKernel.Plugins.KernelMemory.CodeInterpretion.CodeInterpretionPlugin;
using DiscordBot.SemanticKernel.QueneService;
using DiscordBot.SemanticKernel.Plugins.Mabinogi;
using DiscordBot.DataObject;

namespace DiscordBot.SemanticKernel
{
    public class SemanticKernelEngine(ILogger<SemanticKernelEngine> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, MabinogiKernelMemoryFactory mabiKMFactory, PromptHelper promptHelper, EnchantmentHelper enchantmentHelper, ItemHelper itemHelper, AppDbContext appDbContext, IBackgroundTaskQueue taskQueue, DiscordSocketClient client)
    {
        public const string SystemPrompt = "你是一個Discord Bot, 名字叫夏夜小幫手, 你在\"夏夜月涼\"伺服器裡為會員們服務.";

        bool isEngineStarted = false;
        AzureOpenAIConfig chatCompletionConfig;
        AzureOpenAIConfig embeddingConfig;
        CodeInterpretionPluginOptions codeInterpreterConfig;

        public async Task<Kernel> GetKernelAsync(ICollection<LogRecord> logRecords = null, AutoFunctionInvocationFilter autoFunctionInvocationFilter = null, bool withAttachment = false)
        {
            chatCompletionConfig = withAttachment ? semanticKernelConfig.Value.AzureOpenAI.GPT4O : semanticKernelConfig.Value.AzureOpenAI.GPT4oMini;
            embeddingConfig = semanticKernelConfig.Value.AzureOpenAI.Embedding;
            codeInterpreterConfig = semanticKernelConfig.Value.CodeInterpreter;

            // ... initialize the engine ...
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

            builder.Plugins
                .AddFromType<ItemPlugin>()
                .AddFromType<WebPlugin>()
                .AddFromType<TimePlugin>()
                //.AddFromType<TextPlugin>()
                //.AddFromType<WaitPlugin>()
                //.AddFromType<FileIOPlugin>()
                //.AddFromType<SearchUrlPlugin>()
                //.AddFromType<DocumentPlugin>()
                .AddFromType<EnchantmentPlugin>()
                //.AddFromType<TextMemoryPlugin>()
                .AddFromType<CodeInterpretionPlugin>()
                //.AddFromType<Plugins.Web.HttpPlugin>()
                .AddFromType<Plugins.Math.MathPlugin>()
                //.AddFromType<WebFileDownloadPlugin>()
                //.AddFromType<Plugins.About.AboutPlugin>()
                .AddFromPromptDirectory("./SemanticKernel/Plugins/Writer")
                .AddFromType<Plugins.Writer.Summary.ConversationSummaryPlugin>()
                .AddFromObject(new MabiMemoryPlugin(await mabiKMFactory.GetMabinogiKernelMemory(), waitForIngestionToComplete: true), "memory")
                //  TODO: Add Screenshot plugin
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
                .AddSingleton(codeInterpreterConfig)
                .AddSingleton(enchantmentHelper)
                .AddSingleton(itemHelper)
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
                loggingConfiguration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ConsoleTarget());
                loggingConfiguration.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new FileTarget
                {
                    FileName = section.GetValue<string>(NLogConstant.FileName),
                    Layout = section.GetValue<string>(NLogConstant.Layout),
                });

                loggingBuilder.AddNLog(loggingConfiguration);
                loggingBuilder.AddOpenTelemetry(options =>
                {
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

        public async Task<KernelStatus> GenerateResponse(SocketInteraction socketInteraction, string prompt, EventHandler<KernelStatus> onKenelStatusUpdatedCallback, Uri? imageUri = null, ChatHistory? conversationChatHistory = null)
        {
            try
            {
                DateTime startTime = DateTime.Now;

                KernelStatus kernelStatus = new();
                StepStatus pendingStatu = new()
                {
                    DisplayName = nameof(StatusEnum.Pending),
                    Status = StatusEnum.Pending,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = false
                };
                kernelStatus.StepStatuses.Enqueue(pendingStatu);
                Conversation conversation = new()
                {
                    UserPrompt = prompt,
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                };
                kernelStatus.Conversation = conversation;
                onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus);

                SocketGuildUser? user = socketInteraction.User as SocketGuildUser;
                SocketGuildChannel? channel = socketInteraction.Channel as SocketGuildChannel;
                ChatMessageContent result = default;
                ObservableCollection<LogRecord> logRecords = [];
                bool showStatusPerSec = true || socketInteraction.User.Id == ulong.Parse(discordBotConfig.Value.AdminId);
                AutoFunctionInvocationFilter autoFunctionInvocationFilter = new(kernelStatus, onKenelStatusUpdatedCallback, showStatusPerSec: showStatusPerSec);
                Kernel kernel = await GetKernelAsync(logRecords: logRecords, autoFunctionInvocationFilter: autoFunctionInvocationFilter, withAttachment: imageUri != null);
                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

                string basicSystemMessage = $"""
                    使用繁體中文來回覆
                    先使用memory plugin在long term memory裡嘗試尋找答案, 如果找不到(INFO NOT FOUND)才用其他方法 (在memory裡找到的資料需要附上來源和可信度[XX%])
                    如果memory裡沒有相關資料, 可在網上搜尋, 並在回答時附上來源
                    """;
                string basicInfo = $"""
                    你的名字: {client.CurrentUser.Username}
                    目前所在伺服器: {channel?.Guild.Name}
                    目前所在頻道: {channel?.Name}
                    回答風格: 毒舌, 邪惡, 惡毒, 惡意
                    """;
                string currentInfo = $"""
                    目前與你對話的用戶: {user?.DisplayName}
                    目前日期與時間(yyyy-mm-dd HH:mm:ss): {DateTime.Now:yyyy-mm-dd HH:mm:ss}
                    """;

                ChatMessageContentItemCollection userInput = [new TextContent(prompt)];
                if (imageUri != null) userInput.Add(new ImageContent(imageUri));
                ChatHistory history;
                if (conversationChatHistory == null)
                {
                    history = [];
                    history.AddSystemMessage(basicSystemMessage);
                    history.AddSystemMessage(basicInfo);
                }
                else
                {
                    history = conversationChatHistory;
                }
                history.AddSystemMessage(currentInfo);
                history.AddUserMessage(userInput);

                using System.Timers.Timer statusReportTimer = new(1000) { AutoReset = true };
                statusReportTimer.Elapsed += (sender, e) => { onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus); };

                StepStatus thinkingStatus = new()
                {
                    DisplayName = nameof(StatusEnum.Thinking),
                    Status = StatusEnum.Thinking,
                    ShowElapsedTime = showStatusPerSec
                };

                await taskQueue.QueueBackgroundWorkItemAsync(RunWorkloadAsync);
                async ValueTask RunWorkloadAsync(CancellationToken token)
                {
                    startTime = DateTime.Now;
                    thinkingStatus.StartTime = startTime;
                    kernelStatus.StepStatuses = new(kernelStatus.StepStatuses.Where(x => pendingStatu.DisplayName != x.DisplayName));
                    kernelStatus.StepStatuses.Enqueue(thinkingStatus);

                    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                    {
                        Temperature = 0.0,
                        TopP = 0.1,
                        MaxTokens = 4000,
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    };

                    if (showStatusPerSec)
                    {
                        statusReportTimer.Start();
                        onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus);
                    }
                    try
                    {
                        result = await chatCompletionService.GetChatMessageContentAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: kernel, cancellationToken: token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, ex.Message);
                        StepStatus errorStatus = new()
                        {
                            DisplayName = "Internal Error",
                            Status = StatusEnum.Error,
                            ShowElapsedTime = false
                        };
                        kernelStatus.StepStatuses.Enqueue(errorStatus);
                        history.AddUserMessage([new TextContent(ex.Message)]);
                        result = new();
                    }
                    if (showStatusPerSec) statusReportTimer.Stop();
                }

                // Wait for the result
                while (result == default) { await Task.Delay(100); }

                history.AddAssistantMessage($"{Environment.NewLine}{result}");
                StringBuilder sb1 = new();
                foreach (var record in history!.Where(x => x.Role != AuthorRole.System)) sb1.AppendLine(record.ToString());

                kernelStatus.Conversation = new()
                {
                    UserPrompt = prompt,
                    PlanTemplate = sb1.ToString(),
                    Result = ReplaceEmoji($"{result}"),
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                    ChatHistory = history,
                    ChatHistoryJson = history.Serialize(),
                };
                kernelStatus.Conversation.SetTokens(logRecords);
                kernelStatus.StepStatuses = new(kernelStatus.StepStatuses.Where(x => thinkingStatus.DisplayName != x.DisplayName));

                return kernelStatus;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string ReplaceEmoji(string str)
        {
            Dictionary<string, string> mappings = new()
            {
                { "😊","<:mtheart:1199003689705275422>"},
                { "😡","<a:MTAngercry:1148480164137812008>"},
                { "🥲","<:MtCry:1199003857733300324>"},
                { "😭","<:MtCry:1199003857733300324>"},
                { "😳","<:Blaanidhorny:1123216443140489298>"},
            };
            foreach (var mapping in mappings) str = str.Replace(mapping.Key, Emote.Parse(mapping.Value).ToString());
            return str;
        }
    }
}
