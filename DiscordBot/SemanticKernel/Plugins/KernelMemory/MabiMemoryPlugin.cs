﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.SemanticKernelPlugin.Internals;
using Microsoft.SemanticKernel;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory
{
    //
    // 摘要:
    //     Kernel Memory Plugin Recommended name: "memory" Functions: * memory.save * memory.saveFile
    //     * memory.saveWebPage * memory.ask * memory.search * memory.delete
    public class MabiMemoryPlugin
    {
        //
        // 摘要:
        //     Name of the input variable used to specify which memory index to use.
        public const string IndexParam = "index";

        //
        // 摘要:
        //     Name of the input variable used to specify a file path.
        public const string FilePathParam = "filePath";

        //
        // 摘要:
        //     Name of the input variable used to specify a unique id associated with stored
        //     information. Important: the text is stored in memory over multiple records, using
        //     an internal format, and Document ID is used across all the internal memory records
        //     generated. Each of these internal records has an internal ID that is not exposed
        //     to memory clients. Document ID can be used to ask questions about a specific
        //     text, to overwrite (update) the text, and to delete it.
        public const string DocumentIdParam = "documentId";

        //
        // 摘要:
        //     Name of the input variable used to specify a web URL.
        public const string UrlParam = "url";

        //
        // 摘要:
        //     Name of the input variable used to specify a search query.
        public const string QueryParam = "query";

        //
        // 摘要:
        //     Name of the input variable used to specify a question to answer.
        public const string QuestionParam = "question";

        //
        // 摘要:
        //     Name of the input variable used to specify optional tags associated with stored
        //     information. Tags can be used to filter memories over one or multiple keys, e.g.
        //     userID, tenant, groups, project ID, room number, content type, year, region,
        //     etc. Each tag can have multiple values, e.g. to link a memory to multiple users.
        public const string TagsParam = "tags";

        //
        // 摘要:
        //     Name of the input variable used to specify custom memory ingestion steps. The
        //     list is usually: "extract", "partition", "gen_embeddings", "save_records"
        public const string StepsParam = "steps";

        //
        // 摘要:
        //     Name of the input variable used to specify custom minimum relevance for the memories
        //     to retrieve.
        public const string MinRelevanceParam = "minRelevance";

        //
        // 摘要:
        //     Name of the input variable used to specify the maximum number of items to return.
        public const string LimitParam = "limit";

        //
        // 摘要:
        //     Default index where to store and retrieve memory from. When null the service
        //     will use a default index for all information.
        private readonly string? _defaultIndex;

        //
        // 摘要:
        //     Default collection of tags to add to information when ingesting.
        private readonly TagCollection? _defaultIngestionTags;

        //
        // 摘要:
        //     Default collection of tags required when retrieving memory (using filters).
        private readonly TagCollection? _defaultRetrievalTags;

        //
        // 摘要:
        //     Default ingestion steps when storing new memories.
        private readonly List<string>? _defaultIngestionSteps;

        //
        // 摘要:
        //     Whether to wait for the asynchronous ingestion to be complete when storing new
        //     memories. Note: the plugin will wait max Microsoft.KernelMemory.MemoryPlugin._maxIngestionWait
        //     seconds to avoid blocking callers for too long.
        private readonly bool _waitForIngestionToComplete;

        //
        // 摘要:
        //     Max time to wait for ingestion completion when Microsoft.KernelMemory.MemoryPlugin._waitForIngestionToComplete
        //     is set to True.
        private readonly TimeSpan _maxIngestionWait = TimeSpan.FromSeconds(15.0);

        //
        // 摘要:
        //     Client to memory read/write. This is usually an instance of MemoryWebClient but
        //     the plugin allows to inject any IKernelMemory, e.g. in case of custom implementations
        //     and the embedded Serverless client.
        private readonly IKernelMemory _memory;

        //
        // 摘要:
        //     Create new instance using MemoryWebClient pointed at the given endpoint.
        //
        // 參數:
        //   endpoint:
        //     Memory Service endpoint
        //
        //   apiKey:
        //     Memory Service authentication API Key
        //
        //   apiKeyHeader:
        //     Name of the HTTP header used to send the Memory API Key
        //
        //   defaultIndex:
        //     Default Memory Index to use when none is specified. Optional. Can be overridden
        //     on each call.
        //
        //   defaultIngestionTags:
        //     Default Tags to add to memories when importing data. Optional. Can be overridden
        //     on each call.
        //
        //   defaultRetrievalTags:
        //     Default Tags to require when searching memories. Optional. Can be overridden
        //     on each call.
        //
        //   defaultIngestionSteps:
        //     Pipeline steps to use when importing memories. Optional. Can be overridden on
        //     each call.
        //
        //   waitForIngestionToComplete:
        //     Whether to wait for the asynchronous ingestion to be complete when storing new
        //     memories.
        public MabiMemoryPlugin(Uri endpoint, string apiKey = "", string apiKeyHeader = "Authorization", string defaultIndex = "", TagCollection? defaultIngestionTags = null, TagCollection? defaultRetrievalTags = null, List<string>? defaultIngestionSteps = null, bool waitForIngestionToComplete = false)
            : this(new MemoryWebClient(endpoint.AbsoluteUri, apiKey, apiKeyHeader), defaultIndex, defaultIngestionTags, defaultRetrievalTags, defaultIngestionSteps, waitForIngestionToComplete)
        {
        }

        //
        // 摘要:
        //     Create new instance using MemoryWebClient pointed at the given endpoint.
        //
        // 參數:
        //   serviceUrl:
        //     Memory Service endpoint
        //
        //   apiKey:
        //     Memory Service authentication API Key
        //
        //   waitForIngestionToComplete:
        //     Whether to wait for the asynchronous ingestion to be complete when storing new
        //     memories.
        public MabiMemoryPlugin(string serviceUrl, string apiKey = "", bool waitForIngestionToComplete = false)
            : this(new Uri(serviceUrl), apiKey, "Authorization", "", null, null, null, waitForIngestionToComplete)
        {
        }

        //
        // 摘要:
        //     Create a new instance using a custom IKernelMemory implementation.
        //
        // 參數:
        //   memoryClient:
        //     Custom IKernelMemory implementation
        //
        //   defaultIndex:
        //     Default Memory Index to use when none is specified. Optional. Can be overridden
        //     on each call.
        //
        //   defaultIngestionTags:
        //     Default Tags to add to memories when importing data. Optional. Can be overridden
        //     on each call.
        //
        //   defaultRetrievalTags:
        //     Default Tags to require when searching memories. Optional. Can be overridden
        //     on each call.
        //
        //   defaultIngestionSteps:
        //     Pipeline steps to use when importing memories. Optional. Can be overridden on
        //     each call.
        //
        //   waitForIngestionToComplete:
        //     Whether to wait for the asynchronous ingestion to be complete when storing new
        //     memories.
        public MabiMemoryPlugin(IKernelMemory memoryClient, string defaultIndex = "", TagCollection? defaultIngestionTags = null, TagCollection? defaultRetrievalTags = null, List<string>? defaultIngestionSteps = null, bool waitForIngestionToComplete = false)
        {
            _memory = memoryClient;
            _defaultIndex = defaultIndex;
            _defaultIngestionTags = defaultIngestionTags;
            _defaultRetrievalTags = defaultRetrievalTags;
            _defaultIngestionSteps = defaultIngestionSteps;
            _waitForIngestionToComplete = waitForIngestionToComplete;
        }

        //
        // 摘要:
        //     Store text information in long term memory. Usage from prompts: '{{memory.save
        //     ...}}'
        //
        // 傳回:
        //     Document ID
        //[KernelFunction]
        [Description("Store in memory the given text")]
        public async Task<string> SaveAsync([Description("The text to save in memory")] string input, [Description("The document ID associated with the information to save")][DefaultValue(null)] string? documentId = null, [Description("Memories index associated with the information to save")][DefaultValue(null)] string? index = null, [Description("Memories index associated with the information to save")][DefaultValue(null)] TagCollectionWrapper? tags = null, [Description("Steps to parse the information and store in memory")][DefaultValue(null)] ListOfStringsWrapper? steps = null, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            string id = await _memory.ImportTextAsync(input, documentId, index: index ?? _defaultIndex, tags: tags ?? _defaultIngestionTags, steps: steps ?? _defaultIngestionSteps, cancellationToken: cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await WaitForDocumentReadinessAsync(id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return id;
        }

        //
        // 摘要:
        //     Store a file content in long term memory. Usage from prompts: '{{memory.saveFile
        //     ...}}'
        //
        // 傳回:
        //     Document ID
        //[KernelFunction]
        [Description("Store in memory the information extracted from a file")]
        public async Task<string> SaveFileAsync([Description("Path of the file to save in memory")] string filePath, [Description("The document ID associated with the information to save")][DefaultValue(null)] string? documentId = null, [Description("Memories index associated with the information to save")][DefaultValue(null)] string? index = null, [Description("Memories index associated with the information to save")][DefaultValue(null)] TagCollectionWrapper? tags = null, [Description("Steps to parse the information and store in memory")][DefaultValue(null)] ListOfStringsWrapper? steps = null, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            string id = await _memory.ImportDocumentAsync(filePath, documentId, tags ?? _defaultIngestionTags, index ?? _defaultIndex, steps ?? _defaultIngestionSteps, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await WaitForDocumentReadinessAsync(id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return id;
        }

        //[KernelFunction]
        [Description("Store in memory the information extracted from a web page")]
        public async Task<string> SaveWebPageAsync([Description("Complete URL of the web page to save")] string url, [Description("The document ID associated with the information to save")][DefaultValue(null)] string? documentId = null, [Description("Memories index associated with the information to save")][DefaultValue(null)] string? index = null, [Description("Memories index associated with the information to save")][DefaultValue(null)] TagCollectionWrapper? tags = null, [Description("Steps to parse the information and store in memory")][DefaultValue(null)] ListOfStringsWrapper? steps = null, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            string id = await _memory.ImportWebPageAsync(url, documentId, tags ?? _defaultIngestionTags, index ?? _defaultIndex, steps ?? _defaultIngestionSteps, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            await WaitForDocumentReadinessAsync(id, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            return id;
        }

        //[KernelFunction]
        [Description("Return up to N memories related to the input text")]
        public async Task<string> SearchAsync([Description("The text to search in memory")] string query, [Description("Memories index to search for information")][DefaultValue("")] string? index = null, [Description("Minimum relevance of the memories to return")][DefaultValue(0.0)] double minRelevance = 0.0, [Description("Maximum number of memories to return")][DefaultValue(1)] int limit = 1, [Description("Memories tags to search for information")][DefaultValue(null)] TagCollectionWrapper? tags = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            SearchResult searchResult = await _memory.SearchAsync(query, index ?? _defaultIndex, TagsToMemoryFilter(tags ?? _defaultRetrievalTags), null, minRelevance, limit, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
            if (searchResult.Results.Count == 0)
            {
                return string.Empty;
            }

            return (limit == 1) ? searchResult.Results.First().Partitions.First().Text : JsonSerializer.Serialize(searchResult.Results.Select((Citation x) => x.Partitions.First().Text));
        }

        //
        // 摘要:
        //     Answer a question using the information stored in long term memory. Usage from
        //     prompts: '{{memory.ask ...}}'
        //
        // 傳回:
        //     The answer returned by the memory.
        [KernelFunction]
        [Description("Use long term memory to answer a question")]
        public async Task<string> AskAsync([Description("The question to answer")] string question, [Description("Memories index to search for answers")][DefaultValue("")] string? index = null, [Description("Minimum relevance of the sources to consider")][DefaultValue(0.0)] double minRelevance = 0.0, [Description("Memories tags to search for information")][DefaultValue(null)] TagCollectionWrapper? tags = null, ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = (await _memory.AskAsync(question, index ?? _defaultIndex, TagsToMemoryFilter(tags ?? _defaultRetrievalTags), null, minRelevance, cancellationToken).ConfigureAwait(continueOnCapturedContext: false)).Result;
            // TODO: List source
            return result;
        }

        //
        // 摘要:
        //     Remove from memory all the information extracted from the given document ID Usage
        //     from prompts: '{{memory.delete ...}}'
        //[KernelFunction]
        [Description("Remove from memory all the information extracted from the given document ID")]
        public Task DeleteAsync([Description("The document to delete")] string documentId, [Description("Memories index where the document is stored")][DefaultValue("")] string? index = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _memory.DeleteDocumentAsync(documentId, index ?? _defaultIndex, cancellationToken);
        }

        private async Task WaitForDocumentReadinessAsync(string documentId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_waitForIngestionToComplete)
            {
                return;
            }

            using CancellationTokenSource timedTokenSource = new CancellationTokenSource(_maxIngestionWait);
            using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timedTokenSource.Token, cancellationToken);
            _ = 1;
            try
            {
                while (!(await _memory.IsDocumentReadyAsync(documentId, null, linkedTokenSource.Token).ConfigureAwait(continueOnCapturedContext: false)))
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500.0), linkedTokenSource.Token).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private static MemoryFilter? TagsToMemoryFilter(TagCollection? tags)
        {
            if (tags == null)
            {
                return null;
            }

            MemoryFilter memoryFilter = new MemoryFilter();
            foreach (KeyValuePair<string, List<string>> tag in tags)
            {
                memoryFilter.Add(tag.Key, tag.Value);
            }

            return memoryFilter;
        }
    }
}