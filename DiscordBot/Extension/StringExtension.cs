using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

        public static string ToHighLight(this string input)
        {
            return $"`{input}`";
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

        public static string FormatExpression(this string input)
        {
            string output = input;
            output = output.Replace(" ", string.Empty);
            output = output.Replace("+", " + ");
            output = output.Replace("-", " - ");
            output = output.Replace("*", " * ");
            output = output.Replace("/", " / ");
            return output;
        }

        public static Random GetRandomFromSeed(this string input)
        {
            if (string.IsNullOrEmpty(input)) return new Random();
            byte[] textData = Encoding.UTF8.GetBytes(input);
            byte[] hash = SHA256.HashData(textData);
            return new(BitConverter.ToInt32(hash));
        }
    }
}
