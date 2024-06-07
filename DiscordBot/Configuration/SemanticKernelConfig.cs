using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;
using Microsoft.KernelMemory;

namespace DiscordBot.Configuration
{
    public class SemanticKernelConfig
    {
        public const string SectionName = "SemanticKernel";

        public AzureOpenAiConfig AzureOpenAI { get; set; }
        public KernelMemoryConfig KernelMemory { get; set; }
        public GoogleSearchApiConfig GoogleSearchApi { get; set; }
        public ApplicationInsightsConfig ApplicationInsightsConfig { get; set; }

        public bool Validate()
        {
            return AzureOpenAI != null && KernelMemory != null && GoogleSearchApi != null;
        }
    }

    public class AzureOpenAiConfig
    {
        public AzureOpenAIConfig GPT35 { get; set; }
        public AzureOpenAIConfig GPT4V { get; set; }
        public AzureOpenAIConfig GPT4_1106 { get; set; }
        public AzureOpenAIConfig GPT4_32K { get; set; }
        public AzureOpenAIConfig GPT4_Turbo_0409 { get; set; }
        public AzureOpenAIConfig GPT4O { get; set; }
        public AzureOpenAIConfig LLAMA3 { get; set; }
        public AzureOpenAIConfig Embedding { get; set; }
    }

    public class KernelMemoryConfig
    {
        public string TokenEncodingName { get; set; }
        public Ollama Ollama { get; set; }
        public DataSource DataSource { get; set; }
        public DiscordConnectorConfig Discord { get; set; }
    }

    public class Ollama
    {
        public string Url { get; set; }
        public string Model { get; set; }
    }

    public class DataSource
    {
        public Website[] Website { get; set; }
    }

    public class Website
    {
        public string Name { get; set; }
        public string XPath { get; set; }
        public string Url { get; set; }
        public string[] Tag { get; set; }
    }

    public class GoogleSearchApiConfig
    {
        public string ApiKey { get; set; }
        public string SearchEngineId { get; set; }
    }

    public class ApplicationInsightsConfig
    {
        public string ConnectionString { get; set; }
    }
}
