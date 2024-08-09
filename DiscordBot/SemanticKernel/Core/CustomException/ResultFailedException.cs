using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.SemanticKernel.Core.CustomException
{
    public class ResultFailedException(string? message) : Exception(message)
    {
    }
}
