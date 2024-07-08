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
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Web;

namespace DiscordBot.SemanticKernel.Plugins.Web;

/// <summary>
/// Web search  plugin.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WebPlugin"/> class.
/// </remarks>
/// <param name="connector">The web search engine connector.</param>
public sealed class WebPlugin(ILogger<DataScrapingJob> logger, IWebSearchEngineConnector connector, DataScrapingHelper dataScrapingHelper)
{
    /// <summary>
    /// The count parameter name.
    /// </summary>
    public const string CountParam = "count";

    /// <summary>
    /// The offset parameter name.
    /// </summary>
    public const string OffsetParam = "offset";

    /// <summary>
    /// The usage of JavaScriptEncoder.UnsafeRelaxedJsonEscaping here is considered safe in this context
    /// because the JSON result is not used for any security sensitive operations like HTML injection.
    /// </summary>
    private static readonly JsonSerializerOptions s_jsonOptionsCache = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Performs a web search using the provided query, count, and offset.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <param name="count">The number of results to return. Default is 3.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The return value contains the search results as an IEnumerable WebPage object serialized as a string</returns>
    [KernelFunction, Description("Perform a web search with google search.")]
    public async Task<string> GoogleSearch(
        [Description("Text to search for")] string query,
        Kernel kernel,
        [Description("Number of results")] int count = 3,
        CancellationToken cancellationToken = default)
    {
        string response = "INFO NOT FOUND";
        try
        {
            IEnumerable<WebPage> searchResults = await connector.SearchAsync<WebPage>(query, count, 0, cancellationToken);
            searchResults = await dataScrapingHelper.GetWebContent(searchResults);
            //foreach (WebPage webPage in searchResults!)
            //{
            //    string snippetSummary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "FindRelatedInformationWithGoal", new()
            //    {
            //        { "input", webPage.Snippet },
            //        { "goal", query },
            //        { "kernel", kernel },
            //    }, cancellationToken);
            //    webPage.Snippet = snippetSummary!;
            //}
            if (!searchResults.Any())
            {
                throw new InvalidOperationException("Failed to get a response from the web search engine.");
            }

            string resultSnippet = string.Join($"{Environment.NewLine}{Environment.NewLine}", searchResults.Select((x, index) => $"RESULT {index + 1}:{Environment.NewLine}Source Url: {x.Url}{Environment.NewLine}BEGIN RESULT {index + 1} CONTENT:{Environment.NewLine}{x.Snippet}{Environment.NewLine}END RESULT {index + 1} CONTENT"));
            //string finalSummary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "SummarizeConversation", new()
            //    {
            //        { "input", resultSnippet },
            //        { "kernel", kernel },
            //    }, cancellationToken);
            //response = finalSummary!;
            response = resultSnippet!;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            throw;
        }
        return response;
    }

    /// <summary>
    /// Performs a web search using the provided query, count, and offset.
    /// </summary>
    /// <param name="query">The text to search for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The return value contains the search results as an IEnumerable WebPage object serialized as a string</returns>
    [KernelFunction, Description("Perform a web search with Microsoft Bing AI and return complete results.")]
    public async Task<string> BingAiSearch(
        [Description("Text to search for")] string query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string bingChatResult = await dataScrapingHelper.GetBingChatResult(query);
            return bingChatResult;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            return "INFO NOT FOUND";
        }
    }

    /// <summary>
    /// Get content of a website.
    /// </summary>
    /// <param name="url">The URI of the request.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The return value contains the content as a string</returns>
    [KernelFunction, Description("Get content of a website.")]
    public async Task<string> GetWebContent(
        [Description("The URI of the request")] string url,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        string response = "INFO NOT FOUND";
        try
        {
            var content = await dataScrapingHelper.GetWebContent([new WebPage() { Url = url }]);
            if (!content.Any()) throw new InvalidOperationException($"Failed to get content of Url: {url}.");
            response = content.First().Snippet;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
            throw;
        }
        return response;
    }
}
