using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.KernelMemory;

namespace DiscordBot.Configuration
{
    public class SemanticKernelConfig
    {
        public const string SectionName = "SemanticKernel";

        public AzureOpenAI AzureOpenAI { get; set; }
        public KernelMemory KernelMemory { get; set; }
        public GoogleSearchApi GoogleSearchApi { get; set; }

        public bool Validate()
        {
            return AzureOpenAI != null && KernelMemory != null && GoogleSearchApi != null;
        }
    }

    public class AzureOpenAI
    {
        public AzureOpenAIConfig GPT35 { get; set; }
        public AzureOpenAIConfig GPT4 { get; set; }
        public AzureOpenAIConfig Embedding { get; set; }
    }

    public class KernelMemory
    {
        public string TokenEncodingName { get; set; }
        public Ollama Ollama { get; set; }
        public DataSource DataSource { get; set; }
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

    public class GoogleSearchApi
    {
        public string ApiKey { get; set; }
        public string SearchEngineId { get; set; }
    }

}
