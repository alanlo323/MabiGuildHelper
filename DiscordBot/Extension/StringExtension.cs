using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DiscordBot.Extension
{
    public static class StringExtension
    {
        public static string ToQuotation(this string input)
        {
            return $"```{input}```";
        }

        public static string MarkDownEscape(this string input)
        {
            return input
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("~", "\\~")
                .Replace("`", "\\`");
        }

        public static string TrimToDiscordEmbedLimited(this string input)
        {
            int maxLength = 3900;
            if (input.Length > maxLength)
            {
                return $"{input[..maxLength]}...";
            }

            return input;
        }
    }
}
