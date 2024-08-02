using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.SemanticKernel.Plugins.KernelMemory.Extensions.Discord;
using Microsoft.KernelMemory;
using SemanticKernel.Assistants.AutoGen.Plugins;

namespace DiscordBot.Configuration
{
    public class SemanticKernelConfig
    {
        public const string SectionName = "SemanticKernel";

        public required AzureOpenAiConfig AzureOpenAI { get; set; }
        public required KernelMemoryConfig KernelMemory { get; set; }
        public required RabbitMQ RabbitMQ { get; set; }
        public required GoogleSearchApiConfig GoogleSearchApi { get; set; }
        public required CodeInterpretionPluginOptions CodeInterpreter { get; set; }

        public bool Validate()
        {
            return AzureOpenAI != null && KernelMemory != null && GoogleSearchApi != null;
        }
    }

    public class AzureOpenAiConfig
    {
        public required AzureOpenAIConfig GPT35 { get; set; }
        public required AzureOpenAIConfig GPT4V { get; set; }
        public required AzureOpenAIConfig GPT4_1106 { get; set; }
        public required AzureOpenAIConfig GPT4_32K { get; set; }
        public required AzureOpenAIConfig GPT4_Turbo_0409 { get; set; }
        public required AzureOpenAIConfig GPT4O { get; set; }
        public required AzureOpenAIConfig GPT4oMini{ get; set; }
        public required AzureOpenAIConfig LLAMA3 { get; set; }
        public required AzureOpenAIConfig Embedding { get; set; }
    }

    public class KernelMemoryConfig
    {
        public required string TokenEncodingName { get; set; }
        public required Ollama Ollama { get; set; }
        public required DataSource DataSource { get; set; }
        public required DiscordConnectorConfig Discord { get; set; }
    }

    public class Ollama
    {
        public required string Url { get; set; }
        public required string Model { get; set; }
    }

    public class DataSource
    {
        public required Website[] Website { get; set; }
    }

    public class Website
    {
        public required string Name { get; set; }
        public required string XPath { get; set; }
        public required string Url { get; set; }
        public required string[] Tag { get; set; }
    }

    public class RabbitMQ
    {
        public required string Host { get; set; }
        public required int Port { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string VirtualHost { get; set; }
    }

    public class GoogleSearchApiConfig
    {
        public required string ApiKey { get; set; }
        public required string SearchEngineId { get; set; }
    }
}
