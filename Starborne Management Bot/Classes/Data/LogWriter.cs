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
        public static string LogFileLoc { get { return Constants._WORKDIR_ + @"Logs\Log"; } }

        public static async Task WriteLogFile(string logMsg)
        {
            string date = (DateTime.Now.Day.ToString().Length == 2 ? DateTime.Now.Day.ToString() : $"0{DateTime.Now.Day.ToString()}") + "-";
            date += (DateTime.Now.Month.ToString().Length == 2 ? DateTime.Now.Month.ToString() : $"0{DateTime.Now.Month.ToString()}") + "-";
            date += DateTime.Now.Year.ToString();
            string fileLoc = $"{LogFileLoc}-{date}.txt";
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
