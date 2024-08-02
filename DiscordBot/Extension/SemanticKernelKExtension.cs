using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace DiscordBot.Extension
{
    public static class SemanticKernelKExtension
    {
        public static string ToKey(this KernelArguments kernelArguments)
        {
            return kernelArguments == null ? string.Empty : kernelArguments.Select(x => $"{x.Value}").Aggregate((s1, s2) => $"{s1}-{s2}");
        }

        public static string ToDisplayName(this KernelArguments kernelArguments)
        {
            if (kernelArguments == null) return string.Empty;
            string asd = kernelArguments.Select(x => $"{x.Value}").Aggregate((s1, s2) => $"{s1} {s2}");
            return kernelArguments == null ? string.Empty : $"  [{asd}]";
        }
    }
}
