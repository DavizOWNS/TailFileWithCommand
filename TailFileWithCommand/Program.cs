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
            {
                WriteLine("At least 2 arguments are required");
                return 1;
            }
            string tailFilePath = args[0];
            string commandPath = args[1];
            string cmdArgs = string.Empty;
            foreach (var a in args.Skip(2))
                cmdArgs += a + " ";

            if (File.Exists(tailFilePath)) File.Delete(tailFilePath);
            CancellationTokenSource cts = new CancellationTokenSource();
            var tailTask = TailFile(tailFilePath, cts.Token);

            WriteLine("Starting process " + commandPath);
            var proc = Process.Start(new ProcessStartInfo(commandPath, cmdArgs));
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

            long lastFilePosition = 0;
            while(true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = new FileInfo(filePath);
                long fileLength = file.Length;
                if (fileLength > lastFilePosition)
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fs.Seek(lastFilePosition, SeekOrigin.Begin);
                        using (var reader = new StreamReader(fs))
                        {
                            int bytesToRead = (int)(fileLength - lastFilePosition);
                            char[] buffer = new char[bytesToRead];
                            int bytesRead = reader.ReadBlock(buffer, 0, bytesToRead);
                            while(bytesToRead - bytesRead > 0)
                            {
                                bytesRead += reader.ReadBlock(buffer, bytesRead, bytesToRead - bytesRead);
                            }

                            Write(buffer);
                        }
                    }

                    lastFilePosition = fileLength;
                }

                await Task.Delay(500);
            }
            //using (StreamReader reader = new StreamReader(new FileStream(filePath,
            //         FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            //{
            //    //start at the end of the file
            //    long lastMaxOffset = 0;

            //    string line = string.Empty;
            //    while (true)
            //    {
            //        await Task.Delay(100);
            //        cancellationToken.ThrowIfCancellationRequested();

            //        //if the file size has not changed, idle
            //        if (reader.BaseStream.Length == lastMaxOffset)
            //            continue;

            //        //seek to the last max offset
            //        //reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);
            //        //reader.DiscardBufferedData();

            //        //read out of the file until the EOF
            //        int c;
            //        while((c = reader.Peek()) != -1)
            //        {
            //            reader.Read();
            //            lastMaxOffset++;
            //            if (c == '\r')
            //            {
            //                if(reader.Peek() == '\n')
            //                {
            //                    reader.Read();
            //                    lastMaxOffset++;
            //                    WriteLine(line);
            //                    line = string.Empty;
            //                    break;
            //                }
            //            }
            //            if(c == '\n')
            //            {
            //                WriteLine(line);
            //                line = string.Empty;
            //                break;
            //            }

            //            line += (char)c;
            //        }

            //        //update the last max offset
            //        //lastMaxOffset = reader.BaseStream.Position;
            //    }
            //}
        }
    }
}
