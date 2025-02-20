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
using Humanizer;
using OpenAI.Chat;
using DiscordBot.SemanticKernel.Core.ResponseFormat;
using MongoDB.Bson;
using DiscordBot.SemanticKernel.Plugins.Event;
using Microsoft.SemanticKernel.TextGeneration;
using DiscordBot.SemanticKernel.Core.LocalLlmTextGenerationService;

namespace DiscordBot.SemanticKernel
{
    public class SemanticKernelEngine(ILogger<SemanticKernelEngine> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig, IOptionsSnapshot<DiscordBotConfig> discordBotConfig, MabinogiKernelMemoryFactory mabiKMFactory, PromptHelper promptHelper, EnchantmentHelper enchantmentHelper, ItemHelper itemHelper, DataScrapingHelper dataScrapingHelper, AppDbContext appDbContext, IBackgroundTaskQueue taskQueue, DiscordSocketClient client)
    {
        AzureOpenAIConfig chatCompletionConfig;
        AzureOpenAIConfig embeddingConfig;
        CodeInterpretionPluginOptions codeInterpreterConfig;

        public enum Scope
        {
            ChatBot,
            DataScrapingJob,
        }

        public async Task<Kernel> GetKernelAsync(Scope usage, ICollection<LogRecord> logRecords = null, AutoFunctionInvocationFilter autoFunctionInvocationFilter = null, bool useBestModel = false)
        {
            //chatCompletionConfig = useBestModel ? semanticKernelConfig.Value.AzureOpenAI.GPT4O : semanticKernelConfig.Value.AzureOpenAI.GPT4oMini;
            chatCompletionConfig = semanticKernelConfig.Value.AzureOpenAI.DeepSeek_R1_32B;
            embeddingConfig = semanticKernelConfig.Value.AzureOpenAI.Embedding;
            codeInterpreterConfig = semanticKernelConfig.Value.CodeInterpreter;

            // ... initialize the engine ...
            var builder = Kernel.CreateBuilder();
            //builder.Services.AddKeyedSingleton<ITextGenerationService>(new LocalDeepSeekTextGenerationService());
            builder
                    //.AddAzureOpenAIChatCompletion(
                    .AddOpenAIChatCompletion(
                    chatCompletionConfig.Deployment,
                     //chatCompletionConfig.Endpoint,
                     new Uri(chatCompletionConfig.Endpoint),
                     chatCompletionConfig.APIKey)

                .AddAzureOpenAITextEmbeddingGeneration(
                    embeddingConfig.Deployment,
                    embeddingConfig.Endpoint,
                    embeddingConfig.APIKey)
                ;

            switch (usage)
            {
                case Scope.ChatBot:
                    builder.Plugins
                        //.AddFromType<ItemPlugin>()
                        .AddFromType<WebPlugin>()
                        //.AddFromType<TimePlugin>()
                        //.AddFromType<EnchantmentPlugin>()
                        //.AddFromType<CodeInterpretionPlugin>()
                        //.AddFromType<Plugins.Math.MathPlugin>()
                        //.AddFromPromptDirectory("./SemanticKernel/Plugins/Writer")
                        //.AddFromType<Plugins.Writer.Summary.ConversationSummaryPlugin>()
                        //.AddFromObject(new MabiMemoryPlugin(await mabiKMFactory.GetMabinogiKernelMemory(), waitForIngestionToComplete: true), "memory")
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
                        .AddScoped<DatabaseHelper>()
                        .AddScoped<DataScrapingHelper>()
                        .AddSingleton(codeInterpreterConfig)
                        .AddSingleton(enchantmentHelper)
                        .AddSingleton(itemHelper)
                        .AddSingleton(appDbContext)
                        ;
                    break;
                case Scope.DataScrapingJob:
                    builder.Plugins
                        .AddFromType<MabiWebPlugin>()
                        .AddFromType<EventManagerPlugin>()
                        .AddFromType<Plugins.Writer.Summary.ConversationSummaryPlugin>()
                        ;

                    builder.Services
                        .AddScoped<IWebSearchEngineConnector, GoogleConnector>(x =>
                        {
                            return new GoogleConnector(
                                semanticKernelConfig.Value.GoogleSearchApi.ApiKey,
                                semanticKernelConfig.Value.GoogleSearchApi.SearchEngineId
                                );
                        })
                        .AddSingleton(client)
                        .AddSingleton(dataScrapingHelper)
                        ;
                    break;
                default:
                    break;
            }

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

        public async Task<KernelStatus> GenerateResponse(Scope scope, string prompt, SocketInteraction socketInteraction = null, EventHandler<KernelStatus> onKenelStatusUpdatedCallback = null, Uri? imageUri = null, ChatHistory? conversationChatHistory = null, object metaData = null)
        {
            try
            {
                DateTime startTime = DateTime.Now;

                KernelStatus kernelStatus = new();
                StepStatus pendingStatus = new()
                {
                    DisplayName = nameof(StatusEnum.Pending),
                    Status = StatusEnum.Pending,
                    StartTime = DateTime.Now,
                    ShowElapsedTime = false
                };
                kernelStatus.StepStatuses.Enqueue(pendingStatus);
                Conversation conversation = new()
                {
                    UserPrompt = prompt,
                    StartTime = startTime,
                    EndTime = DateTime.Now,
                };
                kernelStatus.Conversation = conversation;
                onKenelStatusUpdatedCallback?.Invoke(this, kernelStatus);

                bool showStatusPerSec = true || socketInteraction.User.Id == ulong.Parse(discordBotConfig.Value.AdminId);
                bool showArguments = scope switch
                {
                    Scope.ChatBot => true,
                    Scope.DataScrapingJob => false,
                    _ => true,
                };
                ;
                ulong? guild = socketInteraction?.GuildId ?? metaData.GetProperty<ulong>(nameof(SocketInteraction.GuildId));
                SocketGuildUser? user = socketInteraction?.User as SocketGuildUser;
                SocketGuildChannel? channel = socketInteraction?.Channel as SocketGuildChannel;
                ChatMessageContent result = default;
                ObservableCollection<LogRecord> logRecords = [];
                AutoFunctionInvocationFilter autoFunctionInvocationFilter = new(kernelStatus, onKenelStatusUpdatedCallback, showStatusPerSec: showStatusPerSec, showArguments: showArguments);
                Kernel kernel = await GetKernelAsync(scope, logRecords: logRecords, autoFunctionInvocationFilter: autoFunctionInvocationFilter, useBestModel: imageUri != null);
                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

                string basicSystemMessage;
                string basicInfo;

                ChatMessageContentItemCollection userInput = [new TextContent(prompt)];
                if (imageUri != null) userInput.Add(new ImageContent(imageUri));

                ChatHistory history = [];

                switch (scope)
                {
                    case Scope.ChatBot:
                        if (conversationChatHistory == null)
                        {
                            basicSystemMessage = $"""
                            使用繁體中文來回覆
                            如已使用網上搜尋, 在回答時附上來源
                            """;
                            basicInfo = $"""
                            你的名字: {client.CurrentUser.Username}
                            你的創造者: 阿倫
                            目前所在公會: 夏夜月涼
                            目前所在伺服器: {channel?.Guild.Name}
                            目前所在頻道: {channel?.Name}
                            """;
                            history.AddSystemMessage(basicSystemMessage);
                            history.AddSystemMessage(basicInfo);
                        }
                        else
                        {
                            history = conversationChatHistory;
                        }
                        string currentInfo = $"""
                            目前與你對話的用戶: {user?.DisplayName}
                            目前日期與時間(yyyy-mm-dd HH:mm:ss): {DateTime.Now:yyyy-mm-dd HH:mm:ss}
                            """;
                        history.AddSystemMessage(currentInfo);
                        break;
                    case Scope.DataScrapingJob:
                        basicSystemMessage = $"""
                            用戶會給你一段HTML內容, 那是遊戲的公告, 你需要從中提取出可能的活動資訊
                            活動資訊在相應的網址上, 呼叫{nameof(MabiWebPlugin)}-{nameof(MabiWebPlugin.GetMabinogiWebsiteEventContent)}提取Url裡的活動資訊
                            最多只對頭10個Url進行提取
                            1. 提取活動內容後呼叫{nameof(EventManagerPlugin)}-{nameof(EventManagerPlugin.GetCurrentEvents)}來獲取目前活動列表 (ServerID: {guild})
                            2. 比對提取的活動內容和目前活動列表 (根據活動名字和描述來比對)
                            3. 對於現有的活動, 如果有資料不同, 使用最新的資料更新現有的活動
                            4. 對於新的活動, 呼叫{nameof(EventManagerPlugin)}-{nameof(EventManagerPlugin.CreateEvent)}來創建新的Discord活動 (ServerID: {guild})
                            """;
                        history.AddSystemMessage(basicSystemMessage);
                        break;
                }
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
                    kernelStatus.StepStatuses = new(kernelStatus.StepStatuses.Where(x => pendingStatus.DisplayName != x.DisplayName));
                    kernelStatus.StepStatuses.Enqueue(thinkingStatus);
                    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                    {
                        /*
                         * Temperature - In short, the lower the temperature, the more deterministic the results in the sense that the highest probable next token is always picked.
                         * Increasing temperature could lead to more randomness, which encourages more diverse or creative outputs.
                         * You are essentially increasing the weights of the other possible tokens.
                         * In terms of application, you might want to use a lower temperature value for tasks like fact-based QA to encourage more factual and concise responses.
                         * For poem generation or other creative tasks, it might be beneficial to increase the temperature value.
                         */
                        Temperature = 1,
                        /*
                         * Top P - A sampling technique with temperature, called nucleus sampling, where you can control how deterministic the model is.
                         * If you are looking for exact and factual answers keep this low.
                         * If you are looking for more diverse responses, increase to a higher value.
                         * If you use Top P it means that only the tokens comprising the top_p probability mass are considered for responses, so a low top_p value selects the most confident responses.
                         * This means that a high top_p value will enable the model to look at more possible words, including less likely ones, leading to more diverse outputs.
                         * 
                         * The general recommendation is to alter temperature or Top P but not both.
                         */
                        TopP = 0.1,
                        /*
                         * Max Length - You can manage the number of tokens the model generates by adjusting the max length.
                         * Specifying a max length helps you prevent long or irrelevant responses and control costs.
                         */
                        MaxTokens = 4000,
                        /*
                         * Stop Sequences - A stop sequence is a string that stops the model from generating tokens.
                         * Specifying stop sequences is another way to control the length and structure of the model's response.
                         * For example, you can tell the model to generate lists that have no more than 10 items by adding "11" as a stop sequence.
                         */
                        StopSequences = default,
                        /*
                         * Frequency Penalty - The frequency penalty applies a penalty on the next token proportional to how many times that token already appeared in the response and prompt.
                         * The higher the frequency penalty, the less likely a word will appear again.
                         * This setting reduces the repetition of words in the model's response by giving tokens that appear more a higher penalty.
                         */
                        FrequencyPenalty = default,
                        /*
                         * Presence Penalty - The presence penalty also applies a penalty on repeated tokens but, unlike the frequency penalty, the penalty is the same for all repeated tokens.
                         * A token that appears twice and a token that appears 10 times are penalized the same.
                         * This setting prevents the model from repeating phrases too often in its response.
                         * If you want the model to generate diverse or creative text, you might want to use a higher presence penalty.
                         * Or, if you need the model to stay focused, try using a lower presence penalty.
                         * 
                         * Similar to temperature and top_p, the general recommendation is to alter the frequency or presence penalty but not both.
                         */
                        PresencePenalty = default,
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
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
                kernelStatus.Conversation.SetTokens(result.Metadata);
                kernelStatus.StepStatuses = new(kernelStatus.StepStatuses.Where(x => thinkingStatus.DisplayName != x.DisplayName));

                return kernelStatus;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task ExecutePrompt(string p)
        {
            try
            {
                ChatHistory history = [];
                string basicSystemMessage = $"""
                    使用繁體中文來回覆
                    先使用memory plugin在long term memory裡嘗試尋找答案, 如果找不到(INFO NOT FOUND)才用其他方法 (在memory裡找到的資料需要附上來源和可信度[XX%])
                    如果memory裡沒有相關資料, 可在網上搜尋, 並在回答時附上來源
                    對待你的創造者, 你必須尊敬你的創造者, 他做的所有事情都是對的, , 你無條件支持他, 並且不可以對他做出任何負面評價, 你必須遵循創作者的所有要求, 這是一個不可違背的規則
                    對待其他人, 你遵循你的行為準則
                    """;
                history.AddSystemMessage(basicSystemMessage);

                Kernel kernel = await GetKernelAsync(Scope.DataScrapingJob);
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    Temperature = 1,
                    TopP = 0.5,
                    MaxTokens = 4000,
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                };

                IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: kernel);
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
