// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using DiscordBot.SemanticKernel.Core.Text;

namespace DiscordBot.SemanticKernel.Plugins.Writer.Summary;
/// <summary>
/// Semantic plugin that enables conversations summarization.
/// </summary>
public class ConversationSummaryPlugin
{
    /// <summary>
    /// The max tokens to process in a single prompt function call.
    /// </summary>
    private const int MaxTokens = 8000;

    private readonly KernelFunction _summarizeConversationFunction;
    private readonly KernelFunction _conversationActionItemsFunction;
    private readonly KernelFunction _conversationTopicsFunction;
    private readonly KernelFunction _findRelatedInformationWithGoalFunction;
    private readonly KernelFunction _summarizeMabiNewsFunction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationSummaryPlugin"/> class.
    /// </summary>
    public ConversationSummaryPlugin()
    {
        PromptExecutionSettings settings = new()
        {
            ExtensionData = new Dictionary<string, object>()
            {
                { "Temperature", 0.1 },
                { "TopP", 0.5 },
                { "MaxTokens", MaxTokens }
            }
        };

        _summarizeConversationFunction = KernelFunctionFactory.CreateFromPrompt(
            PromptFunctionConstants.SummarizeConversationDefinition,
            functionName: nameof(SummarizeConversationAsync).Replace("Async", string.Empty),
            description: "Given a section of a conversation transcript, summarize the part of the conversation.",
            executionSettings: settings);

        _conversationActionItemsFunction = KernelFunctionFactory.CreateFromPrompt(
            PromptFunctionConstants.GetConversationActionItemsDefinition,
            functionName: nameof(GetConversationActionItemsAsync).Replace("Async", string.Empty),
            description: "Given a section of a conversation transcript, identify action items.",
            executionSettings: settings);

        _conversationTopicsFunction = KernelFunctionFactory.CreateFromPrompt(
            PromptFunctionConstants.GetConversationTopicsDefinition,
            functionName: nameof(GetConversationTopicsAsync).Replace("Async", string.Empty),
            description: "Analyze a conversation transcript and extract key topics worth remembering.",
            executionSettings: settings);

        _findRelatedInformationWithGoalFunction = KernelFunctionFactory.CreateFromPrompt(
            PromptFunctionConstants.FindRelatedInformationWithGoalDefinition,
            functionName: nameof(FindRelatedInformationWithGoalAsync).Replace("Async", string.Empty),
            description: "Analyze conversation transcripts and extract target-related information.",
            executionSettings: settings);

        _summarizeMabiNewsFunction = KernelFunctionFactory.CreateFromPrompt(
            PromptFunctionConstants.SummarizeMabiNewsDefinition,
            functionName: nameof(SummarizeMabiNewsAsync).Replace("Async", string.Empty),
            description: "Given a section of a conversation transcript, summarize the part of the conversation.",
            executionSettings: settings);
    }

    /// <summary>
    /// Given a long conversation transcript, summarize the conversation.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Given a long conversation transcript, summarize the conversation.")]
    public Task<string> SummarizeConversationAsync(
        [Description("A long conversation transcript.")] string input,
        Kernel kernel) =>
        ProcessAsync(_summarizeConversationFunction, input, kernel);

    /// <summary>
    /// Given a long conversation transcript, identify action items.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Given a long conversation transcript, identify action items.")]
    public Task<string> GetConversationActionItemsAsync(
        [Description("A long conversation transcript.")] string input,
        Kernel kernel) =>
        ProcessAsync(_conversationActionItemsFunction, input, kernel);

    /// <summary>
    /// Given a long conversation transcript, identify topics.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Given a long conversation transcript, identify topics worth remembering.")]
    public Task<string> GetConversationTopicsAsync(
        [Description("A long conversation transcript.")] string input,
        Kernel kernel) =>
        ProcessAsync(_conversationTopicsFunction, input, kernel);

    /// <summary>
    /// Given a long conversation transcript, identify topics.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Analyze conversation transcripts and extract target-related information.")]
    public async Task<string> FindRelatedInformationWithGoalAsync(
        [Description("A long conversation transcript.")] string input,
        [Description("The goal to find.")] string goal,
        Kernel kernel)
    {
        List<string> lines = TextChunker.SplitPlainTextLines(input, MaxTokens);
        List<string> paragraphs = TextChunker.SplitPlainTextParagraphs(lines, MaxTokens);

        string[] results = new string[paragraphs.Count];

        for (int i = 0; i < results.Length; i++)
        {
            // The first parameter is the input text.
            results[i] = (await _findRelatedInformationWithGoalFunction.InvokeAsync(kernel, new()
            {
                ["input"] = paragraphs[i],
                ["goal"] = goal
            }))
                .GetValue<string>() ?? string.Empty;
        }

        return string.Join("\n", results);
    }

    /// <summary>
    /// Given a long conversation transcript, summarize the conversation.
    /// </summary>
    /// <param name="input">A long conversation transcript.</param>
    /// <param name="kernel">The <see cref="Kernel"/> containing services, plugins, and other state for use throughout the operation.</param>
    [KernelFunction, Description("Summarize the mabinogi news.")]
    public Task<string> SummarizeMabiNewsAsync(
        [Description("Content to summarize")] string input,
        Kernel kernel) =>
        ProcessAsync(_summarizeMabiNewsFunction, input, kernel);

    private static async Task<string> ProcessAsync(KernelFunction func, string input, Kernel kernel)
    {
        List<string> lines = TextChunker.SplitPlainTextLines(input, MaxTokens);
        List<string> paragraphs = TextChunker.SplitPlainTextParagraphs(lines, MaxTokens);

        string[] results = new string[paragraphs.Count];

        for (int i = 0; i < results.Length; i++)
        {
            // The first parameter is the input text.
            results[i] = (await func.InvokeAsync(kernel, new() { ["input"] = paragraphs[i] }).ConfigureAwait(false))
                .GetValue<string>() ?? string.Empty;
        }

        return string.Join("\n", results);
    }
}
