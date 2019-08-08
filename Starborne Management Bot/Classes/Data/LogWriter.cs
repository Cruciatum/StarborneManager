using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Starborne_Management_Bot.Classes.Data
{
    public static class LogWriter
    {
        public static string LogFileLoc { get { return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location).Replace(@"bin\Debug\netcoreapp2.2", @"Logs\Log"); } }

        public static async Task WriteLogFile(string logMsg)
        {
            string fileLoc = $"{LogFileLoc}{DateTime.Now.Date.Day}-{DateTime.Now.Date.Month}-{DateTime.Now.Date.Year} .txt";
            if (!File.Exists(fileLoc))
            {
                File.WriteAllText(fileLoc, $"Logfile for {DateTime.Now.Date}{Environment.NewLine}");
            }
            using (var w = File.AppendText(fileLoc))
            {
                await w.WriteLineAsync(logMsg);
            }
        }
    }
}
