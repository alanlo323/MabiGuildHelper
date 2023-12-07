using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Helper
{
    public class AppDataHelper
    {
        public static string AppDatePath { get; } = "AppData";
        public static string FunnyResponseImagePath { get; } = "FunnyResponseImage";

        public FileInfo GetFunnyResponseFile(string type = null)
        {
            string path = Path.Combine(AppDatePath, FunnyResponseImagePath);
            //path = Path.Combine(path, type);
            var files = Directory.GetFiles(path);
            return new FileInfo(files[Random.Shared.Next(files.Length)]);
        }
    }
}
