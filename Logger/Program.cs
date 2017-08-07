using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    class Program
    {
        static void Main(string[] args)
        {
            string logFilePath = args[0];

            using (var writer = new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)))
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line.StartsWith("end"))
                        return;
                    writer.WriteLine(line);
                    writer.Flush();
                }
            }
        }
    }
}
