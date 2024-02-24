using System.Diagnostics;

namespace MorrighanStarter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                SetupClientExe();
            }
            else
            {
                LogArgs(args);
            }
        }

        private static void SetupClientExe()
        {
            FileInfo backupClient = new FileInfo("client.exe.bak");
            if (!backupClient.Exists)
            {
                FileInfo officalClient = new FileInfo("client.exe");
                officalClient.CopyTo("client.exe.bak", false);
            }
            // get current exe path

            string path = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("dll", "exe");
            FileInfo thisClient = new FileInfo(path);
            thisClient.CopyTo("client.exe", true);

            WaitForClientStart();
        }

        private static void WaitForClientStart()
        {
            FileInfo argsFile = new FileInfo("MorrighanStarter.txt");
            if (argsFile.Exists) argsFile.Delete();
            argsFile.Create().Close();
            DateTime argsTxtCreateTime = argsFile.CreationTime;

            Console.WriteLine($"WaitForClientStart");
            while (true)
            {
                FileInfo argsFile2 = new FileInfo("MorrighanStarter.txt");
                if (argsFile2.LastWriteTime > argsTxtCreateTime)
                {
                    Console.WriteLine("args.txt updated");
                    Console.WriteLine(argsFile2.LastWriteTime);
                    Console.WriteLine(argsTxtCreateTime);
                    // read all lines
                    string[] args = File.ReadAllLines(argsFile2.FullName);
                    StartMorrighan(args);
                    break;
                }
                Thread.Sleep(250);
            }
        }

        private static void LogArgs(string[] args)
        {
            Console.WriteLine($"LogArgs {string.Join(" ", args)}");
            FileInfo argsFile = new FileInfo("MorrighanStarter.txt");
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
        }

        private static void StartMorrighan(string[] args)
        {
            Console.WriteLine($"StartMorrighan");
            FileInfo backupClient = new FileInfo("client.exe.bak");
            if (!backupClient.Exists)
            {
                throw new FileNotFoundException("client.exe.bak not found");
            }
            backupClient.CopyTo("client.exe", true);
            backupClient.Delete();

            // start morrighan with args
            Process.Start("Morrighan.exe", string.Join(" ", args));
        }
    }
}
