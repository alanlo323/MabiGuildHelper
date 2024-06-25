using System.Diagnostics;
using System.Net;

namespace FakeClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await LogArgs(args);
        }

        private static async Task LogArgs(string[] args)
        {
            Console.WriteLine($"LogArgs {string.Join(" ", args)}");
            FileInfo argsFile = new ("LoginData.txt");
            // write args to file
            using StreamWriter sw = argsFile.CreateText();
            foreach (string arg in args)
            {
                string arg2 = arg;
                if (arg2.StartsWith("setting"))
                {
                    var arg3 = arg.Replace("setting:", "");
                    arg2 = $"setting:\"{arg3}\"";
                }
                sw.WriteLine(arg2);
            }
            sw.Close();

            await CallApi(argsFile);
        }

        static async Task CallApi(FileInfo argsFile)
        {
            Console.WriteLine("Calling api to start the real client");
            using HttpClient client = new();
            string url = $"http://localhost:4042/StartClient/{WebUtility.UrlEncode(argsFile.FullName)}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);
        }
    }
}
