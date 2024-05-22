// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    [KernelFunction, Description("Perform a web search and return complete results in JSON format.")]
    public async Task<string> Search(
        [Description("Text to search for")] string query,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<WebPage>? results = null;
        try
        {
            //string googleSearchPrompt = "Searching the web for information...";
            //var googleSearchStr = await  kernel.InvokePromptAsync("Searching the web for information...", cancellationToken: cancellationToken);
            IEnumerable<WebPage> searchResults = await connector.SearchAsync<WebPage>(query, 1, 0, cancellationToken);
            searchResults = await dataScrapingHelper.GetWebContent(searchResults);
            foreach (WebPage webPage in searchResults!)
            {
                string summary = await kernel.InvokeAsync<string>("ConversationSummaryPlugin", "FindRelatedInformationWithGoal", new()
                {
                    { "input", webPage.Snippet },
                    { "goal", query },
                    { "kernel", kernel },
                }, cancellationToken);
                webPage.Snippet = summary!;
            }
            if (!searchResults.Any())
            {
                throw new InvalidOperationException("Failed to get a response from the web search engine.");
            }
            results = searchResults;
        }
        catch (Exception ex)
        {
            logger.LogException(ex);
        }

        string result = string.Join($"{Environment.NewLine}{Environment.NewLine}", results.Select((x, index) => $"RESULT {index + 1}:{Environment.NewLine}{x.Snippet}"));
        return result;
    }
}
