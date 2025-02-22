﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.TextGeneration;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DiscordBot.Configuration;

namespace DiscordBot.SemanticKernel.Core.LocalLlmTextGenerationService
{

    public class LocalDeepSeekTextGenerationService(ILogger<LocalDeepSeekTextGenerationService> logger, IOptionsSnapshot<SemanticKernelConfig> semanticKernelConfig) : ITextGenerationService
    {
        private const string LLMResultText = @"...output from your custom model... Example:
AI is awesome because it can help us solve complex problems, enhance our creativity,
and improve our lives in many ways. AI can perform tasks that are too difficult,
tedious, or dangerous for humans, such as diagnosing diseases, detecting fraud, or
exploring space. AI can also augment our abilities and inspire us to create new forms
of art, music, or literature. AI can also improve our well-being and happiness by
providing personalized recommendations, entertainment, and assistance. AI is awesome.";

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public async IAsyncEnumerable<StreamingTextContent> GetStreamingTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (string word in LLMResultText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                await Task.Delay(50, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                yield return new StreamingTextContent($"{word} ");
            }
        }

        public Task<IReadOnlyList<TextContent>> GetTextContentsAsync(string prompt, PromptExecutionSettings? executionSettings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TextContent>>(
            [
                new(LLMResultText)
            ]);
        }
    }
}