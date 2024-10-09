// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Search.Documents.Models;
using DiscordBot.Extension;
using DiscordBot.Helper;
using DiscordBot.SchedulerJob;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using static DiscordBot.SemanticKernel.SemanticKernelEngine;

namespace DiscordBot.SemanticKernel.Plugins.Web;

/// <summary>
/// Web search  plugin.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MabiWebPlugin"/> class.
/// </remarks>
/// <param name="connector">The web search engine connector.</param>
public sealed class MabiWebPlugin(ILogger<MabiWebPlugin> logger, DataScrapingHelper dataScrapingHelper)
{

    /// <summary>
    /// Get content of a website.
    /// </summary>
    /// <param name="url">The URI of the request.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The return value contains the content as a string</returns>
    [KernelFunction, Description("Get content of Mabinogi event.")]
    public async Task<string> GetMabinogiWebsiteEventContent(
        [Description("The URI of the request")] string url,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            //url = "https://tw-event.beanfun.com/mabinogi/EventAD/EventAD.aspx?EventADID=10825";

            var data = await dataScrapingHelper.GetWebsiteScreenshot(url);
            if (data == null) return $"Unable to get event from url: {url}";

            ChatMessageContentItemCollection userInput = [new ImageContent(data, MimeTypes.ImagePng)];

            ChatHistory history = [];
            string basicSystemMessage = $"""
                    使用繁體中文來回覆
                    獲取圖片中的活動內容
                     - 活動名字
                     - 開始時間
                     - 結束時間
                     - 活動地點
                     - 活動描述
                    """;
            history.AddSystemMessage(basicSystemMessage);
            history.AddUserMessage(userInput);

            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                Temperature = 0,
                TopP = 0.1,
                MaxTokens = 4000,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };

            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: kernel);

            return result.ToString();
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            throw;
        }
    }
}
