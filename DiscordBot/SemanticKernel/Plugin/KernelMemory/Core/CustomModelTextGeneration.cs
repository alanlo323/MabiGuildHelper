using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI;
using OllamaSharp;
using TiktokenSharp;

namespace DiscordBot.SemanticKernel.Plugin.KernelMemory.Core
{
    public class CustomModelConfig
    {
        public int MaxToken { get; set; } = 4096;
        public string TokenEncodingName { get; set; } = string.Empty;

        public string SystemPrompt { get; set; } = "你是一個Discord Bot, 名字叫夏夜小幫手, 你在\"夏夜月涼\"伺服器裡為會員們服務. 你正在幫助的用戶來自台灣, 會講繁體中文.";
        public string InstructPrompt { get; set; } = """
                                              <s>
                                              {{SystemPrompt}}
                                              </s>
                                              [INST]
                                              {{Prompt}}
                                              [/INST]
                                              """;
    }

    public class CustomModelTextGeneration(OllamaApiClient ollama, CustomModelConfig config) : ITextGenerator
    {
        /// <inheritdoc />
        public int MaxTokenTotal { get; } = config.MaxToken;
        public string TokenEncodingName { get; } = config.TokenEncodingName;

        /// <inheritdoc />
        public int CountTokens(string text)
        {
            // ... calculate and return the number of tokens ...

            TikToken tikToken = TikToken.GetEncoding(TokenEncodingName);
            var tokens = tikToken.Encode(text);
            return tokens.Count;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<string> GenerateTextAsync(
            string prompt,
            TextGenerationOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string instructPrompt = config.InstructPrompt.Replace("{{SystemPrompt}}", config.SystemPrompt).Replace("{{Prompt}}", prompt);

            bool done = false;
            object lockObj = new();
            ConcurrentQueue<string> result = new();

            ConversationContext context = null;
            Thread thread = new(async () =>
            {
                context = await ollama.StreamCompletion(prompt, context, stream =>
                {
                    result.Enqueue(stream.Response);
                    done = stream.Done;
                }, cancellationToken: cancellationToken);
            });
            thread.Start();

            while (!done)
            {
                if (result.TryDequeue(out var item)) yield return item;
            }

            yield break;
        }
    }
}
