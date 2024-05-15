// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordBot.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;

/// <summary>
/// Service responsible for connecting to Discord, listening for messages
/// and generating events for Kernel Memory.
/// </summary>
public sealed class DiscordConnector : IHostedService, IDisposable, IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly IKernelMemory _memory;
    private readonly ILogger<DiscordConnector> _log;
    private readonly string _contentStorageIndex;
    private readonly string _contentStorageFilename;
    private readonly List<string> _pipelineSteps;

    /// <summary>
    /// New instance of Discord bot
    /// </summary>
    /// <param name="config">Discord settings</param>
    /// <param name="memory">Memory instance used to upload files when messages arrives</param>
    /// <param name="logFactory">App log factory</param>
    public DiscordConnector(
        IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig,
        DiscordSocketClient client,
        MabinogiKernelMemoryFactory mabinogiKernelMemoryFactory,
        ILoggerFactory logFactory)
    {
        _log = logFactory.CreateLogger<DiscordConnector>();

        var dc = new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Debug,
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            LogGatewayIntentWarnings = true,
            SuppressUnknownDispatchWarnings = false
        };

        var discordConnectorConfig = semanticKernelConfig.Value.KernelMemory.Discord;
        _client = client;
        //_client.Log += OnLog;
        _client.MessageReceived += OnMessage;
        _memory = mabinogiKernelMemoryFactory.GetMabinogiKernelMemory().GetAwaiter().GetResult();
        _contentStorageIndex = discordConnectorConfig.Index;
        _pipelineSteps = discordConnectorConfig.Steps;
        _contentStorageFilename = discordConnectorConfig.FileName;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _client.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
    }

    #region private

    private static readonly Dictionary<LogSeverity, LogLevel> s_logLevels = new()
    {
        [LogSeverity.Critical] = LogLevel.Critical,
        [LogSeverity.Error] = LogLevel.Error,
        [LogSeverity.Warning] = LogLevel.Warning,
        [LogSeverity.Info] = LogLevel.Information,
        [LogSeverity.Verbose] = LogLevel.Debug, // note the inconsistency
        [LogSeverity.Debug] = LogLevel.Trace // note the inconsistency
    };

    private Task OnMessage(SocketMessage message)
    {
        var msg = new DiscordMessage
        {
            MessageId = message.Id.ToString(CultureInfo.InvariantCulture),
            AuthorId = message.Author.Id.ToString(CultureInfo.InvariantCulture),
            ChannelId = message.Channel.Id.ToString(CultureInfo.InvariantCulture),
            ReferenceMessageId = message.Reference?.MessageId.ToString() ?? string.Empty,
            AuthorUsername = message.Author.Username,
            ChannelName = message.Channel.Name,
            Timestamp = message.Timestamp,
            Content = message.Content,
            CleanContent = message.CleanContent,
            EmbedsCount = message.Embeds.Count,
        };

        if (message.Channel is SocketTextChannel textChannel)
        {
            msg.ChannelMention = textChannel.Mention;
            msg.ChannelTopic = textChannel.Topic;
            msg.ServerId = textChannel.Guild.Id.ToString(CultureInfo.InvariantCulture);
            msg.ServerName = textChannel.Guild.Name;
            msg.ServerDescription = textChannel.Guild.Description;
            msg.ServerMemberCount = textChannel.Guild.MemberCount;
        }

        _log.LogTrace("[{0}] New message from '{1}' [{2}]", msg.MessageId, msg.AuthorUsername, msg.AuthorId);
        _log.LogTrace("[{0}] Channel: {1}", msg.MessageId, msg.ChannelId);
        _log.LogTrace("[{0}] Channel: {1}", msg.MessageId, msg.ChannelName);
        _log.LogTrace("[{0}] Timestamp: {1}", msg.MessageId, msg.Timestamp);
        _log.LogTrace("[{0}] Content: {1}", msg.MessageId, msg.Content);
        _log.LogTrace("[{0}] CleanContent: {1}", msg.MessageId, msg.CleanContent);
        _log.LogTrace("[{0}] Reference: {1}", msg.MessageId, msg.ReferenceMessageId);
        _log.LogTrace("[{0}] EmbedsCount: {1}", msg.MessageId, msg.EmbedsCount);
        if (message.Embeds.Count > 0)
        {
            foreach (Embed? x in message.Embeds)
            {
                if (x == null) { continue; }

                _log.LogTrace("[{0}] Embed Title: {1}", message.Id, x.Title);
                _log.LogTrace("[{0}] Embed Url: {1}", message.Id, x.Url);
                _log.LogTrace("[{0}] Embed Description: {1}", message.Id, x.Description);
            }
        }

        Task.Run(async () =>
        {
            string documentId = $"{msg.ServerId}_{msg.ChannelId}_{msg.MessageId}";
            string content = JsonSerializer.Serialize(msg);
            Stream fileContent = new MemoryStream(Encoding.UTF8.GetBytes(content), false);
            await using (fileContent.ConfigureAwait(false))
            {
                try
                {
                    await _memory.ImportDocumentAsync(
                        fileContent,
                        fileName: _contentStorageFilename,
                        documentId: documentId,
                        index: _contentStorageIndex,
                        steps: _pipelineSteps).ConfigureAwait(false);
                }
                catch (Exception ex)
                {

                    throw;
                }
            }
        });

        return Task.CompletedTask;
    }

    private Task OnLog(LogMessage msg)
    {
        var logLevel = LogLevel.Information;
        if (s_logLevels.TryGetValue(msg.Severity, out LogLevel value))
        {
            logLevel = value;
        }

        _log.Log(logLevel, "{0}: {1}", msg.Source, msg.Message);

        return Task.CompletedTask;
    }

    #endregion
}