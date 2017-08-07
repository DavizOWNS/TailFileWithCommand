using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static System.Console;

namespace TailFileWithCommand
{
    class Program
    {
        static void Main(string[] args)
        {
            int exitCode = MainAsync(args).GetAwaiter().GetResult();

            Environment.Exit(exitCode);
        }

        static async Task<int> MainAsync(string[] args)
        {
            if (args.Length < 2)
                return 1;
            string tailFilePath = args[0];
            string commandPath = args[1];
            string cmdArgs = string.Empty;
            foreach (var a in args.Skip(2))
                cmdArgs += a + " ";

            if (File.Exists(tailFilePath)) File.Delete(tailFilePath);
            CancellationTokenSource cts = new CancellationTokenSource();
            var tailTask = TailFile(tailFilePath, cts.Token);

            WriteLine("Starting process " + commandPath);
            var proc = Process.Start(new ProcessStartInfo(commandPath, cmdArgs)
            {
            });
            var procTask = Task.Factory.StartNew(() =>
            {
                proc.WaitForExit();
                Thread.Sleep(1000);
                cts.Cancel();
            });

            try
            {
                await Task.WhenAll(tailTask, procTask);
            }
            catch (OperationCanceledException) { }

            return proc.ExitCode;
        }

        public static async Task TailFile(string filePath, CancellationToken cancellationToken)
        {
            while(!File.Exists(filePath))
            {
                await Task.Delay(1000);
                cancellationToken.ThrowIfCancellationRequested();
            }
            using (StreamReader reader = new StreamReader(new FileStream(filePath,
                     FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                //start at the end of the file
                long lastMaxOffset = 0;

                while (true)
                {
                    await Task.Delay(100);
                    cancellationToken.ThrowIfCancellationRequested();

                    //if the file size has not changed, idle
                    if (reader.BaseStream.Length == lastMaxOffset)
                        continue;

                    //seek to the last max offset
                    reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);

                    //read out of the file until the EOF
                    string line = "";
                    while ((line = reader.ReadLine()) != null)
                        WriteLine(line);

                    //update the last max offset
                    lastMaxOffset = reader.BaseStream.Position;
                }
            }
        }
    }
}
