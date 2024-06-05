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
/// Initializes a new instance of the <see cref="WebSearchPlugin"/> class.
/// </remarks>
/// <param name="connector">The web search engine connector.</param>
public sealed class WebSearchPlugin(ILogger<DataScrapingJob> logger, IWebSearchEngineConnector connector, DataScrapingHelper dataScrapingHelper)
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
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The return value contains the search results as an IEnumerable WebPage object serialized as a string</returns>
    //[KernelFunction, Description("Perform a web search and return complete results.")]
    public async Task<string> Search(
        [Description("Text to search for")] string query,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        string response = "INFO NOT FOUND";
        try
        {

            IEnumerable<WebPage> searchResults = await connector.SearchAsync<WebPage>(query, 3, 0, cancellationToken);
            searchResults = await dataScrapingHelper.GetWebContent(searchResults);
            foreach (WebPage webPage in searchResults!)
            {
                string snippetSummary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "FindRelatedInformationWithGoal", new()
                {
                    { "input", webPage.Snippet },
                    { "goal", query },
                    { "kernel", kernel },
                }, cancellationToken);
                webPage.Snippet = snippetSummary!;
            }
            if (!searchResults.Any())
            {
                throw new InvalidOperationException("Failed to get a response from the web search engine.");
            }

            string resultSnippet = string.Join($"{Environment.NewLine}{Environment.NewLine}", searchResults.Select((x, index) => $"RESULT {index + 1}:{Environment.NewLine}Source Url: {x.Url}{Environment.NewLine}Content:{Environment.NewLine}{x.Snippet}"));
            string finalSummary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "SummarizeConversation", new()
                {
                    { "input", resultSnippet },
                    { "kernel", kernel },
                }, cancellationToken);
            response = finalSummary!;
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
    [KernelFunction, Description("Perform a web search and return complete results.")]
    public async Task<string> BingSearch(
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
}
