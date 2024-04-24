using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Configuration
{
    public class KernelMemoryConfig
    {
        public const string SectionName = "KernelMemory";

        public string TokenEncodingName { get; set; }
        public Ollama Ollama { get; set; }
        public DataSource DataSource { get; set; }

        public bool Validate()
        {
            return !string.IsNullOrEmpty(TokenEncodingName) && Ollama != null;
        }
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
}
