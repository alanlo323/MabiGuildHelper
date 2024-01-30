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
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            FileInfo thisClient = new FileInfo(path);
            thisClient.CopyTo("client.exe", true);

            WaitForClientStart();
        }

        private static void WaitForClientStart()
        {
            FileInfo argsFile = new FileInfo("args.txt");
            if (argsFile.Exists) argsFile.Delete();
            argsFile.Create().Close();
            DateTime argsTxtCreateTime = argsFile.CreationTime;

            while (true)
            {
                FileInfo argsFile2 = new FileInfo("args.txt");
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
            FileInfo argsFile = new FileInfo("args.txt");
            // write args to file
            using StreamWriter sw = argsFile.CreateText();
            foreach (string arg in args)
            {
                sw.WriteLine(arg);
            }
            sw.Close();
        }

        private static void StartMorrighan(string[] args)
        {
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
