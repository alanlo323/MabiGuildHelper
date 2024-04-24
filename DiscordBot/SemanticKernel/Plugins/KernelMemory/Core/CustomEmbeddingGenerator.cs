using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory;
using OllamaSharp;
using TiktokenSharp;

namespace DiscordBot.SemanticKernel.Plugins.KernelMemory.Core
{

    public class CustomEmbeddingGeneratorConfig
    {
        public int MaxToken { get; set; } = 4096;
        public string TokenEncodingName { get; set; } = string.Empty;
    }

    public class CustomEmbeddingGenerator(OllamaApiClient ollama, CustomEmbeddingGeneratorConfig config) : ITextEmbeddingGenerator
    {

        /// <inheritdoc />
        public int MaxTokens { get; } = config.MaxToken;
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
        public async Task<Embedding> GenerateEmbeddingAsync(
            string text, CancellationToken cancellationToken = default)
        {
            OllamaSharp.Models.GenerateEmbeddingResponse result = await ollama.GenerateEmbeddings(text, cancellationToken);
            return new Embedding(result.Embedding.Select(x => (float)x).ToArray());
        }
    }
}
