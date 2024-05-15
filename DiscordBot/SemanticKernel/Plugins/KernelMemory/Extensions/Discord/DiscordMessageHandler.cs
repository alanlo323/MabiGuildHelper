// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using DiscordBot.Configuration;
using DiscordBot.Db;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.Pipeline;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;

/// <summary>
/// KM pipeline handler fetching discord data files from content storage
/// and storing messages in Postgres.
/// </summary>
public sealed class DiscordMessageHandler(
    string stepName,
    IPipelineOrchestrator orchestrator,
    IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig,
    IServiceProvider serviceProvider,
    ILoggerFactory? loggerFactory = null) : IPipelineStepHandler
{
    // Name of the file where to store Discord data
    private readonly string _filename = semanticKernelConfig.Value.KernelMemory.Discord.FileName;

    // .NET logger
    private readonly ILogger<DiscordMessageHandler> _log = loggerFactory?.CreateLogger<DiscordMessageHandler>() ?? DefaultLogger<DiscordMessageHandler>.Instance;

    public string StepName { get; } = stepName;

    public async Task<(bool success, DataPipeline updatedPipeline)> InvokeAsync(DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        // Note: use a new DbContext instance each time, because DbContext is not thread safe and would throw the following
        // exception: System.InvalidOperationException: a second operation was started on this context instance before a previous
        // operation completed. This is usually caused by different threads concurrently using the same instance of DbContext.
        // For more information on how to avoid threading issues with DbContext, see https://go.microsoft.com/fwlink/?linkid=2097913.
        AppDbContext db = serviceProvider.GetRequiredService<AppDbContext>();
        ArgumentNullExceptionEx.ThrowIfNull(db, nameof(db), "Discord DB context is NULL");

        foreach (DataPipeline.FileDetails uploadedFile in pipeline.Files)
        {
            // Process only the file containing the discord data
            if (uploadedFile.Name != this._filename) { continue; }

            string fileContent = await orchestrator.ReadTextFileAsync(pipeline, uploadedFile.Name, cancellationToken);

            DiscordDbMessage? data;
            try
            {
                data = JsonSerializer.Deserialize<DiscordDbMessage>(fileContent);
                if (data == null)
                {
                    this._log.LogError("Failed to deserialize Discord data file, result is NULL");
                    return (true, pipeline);
                }
            }
            catch (Exception e)
            {
                this._log.LogError(e, "Failed to deserialize Discord data file");
                return (true, pipeline);
            }

            await db.Messages.AddAsync(data, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return (true, pipeline);
    }
}
