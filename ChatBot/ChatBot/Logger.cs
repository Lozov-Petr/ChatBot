using System;
using System.IO;

namespace ChatBot
{
    public static class Logger
    {
        public static string CreateLog()
        {
            if (!Directory.Exists("Logs")) Directory.CreateDirectory("Logs");
            var now = DateTime.Now;
            var logname = string.Format("Logs/Log {0}-{1}-{2}--{3}-{4}-{5}.log", 
                now.Year, Create2Digits(now.Month), Create2Digits(now.Day),
                Create2Digits(now.Hour), Create2Digits(now.Minute), Create2Digits(now.Second));
            var stream = File.Create(logname);
            stream.Close();
            return logname;
        }

        public static void WriteInLog(string path, string msg)
        {
            var writer  = new StreamWriter(path, true);
            var fullMsg = string.Format("{0}  {1}\r\n", DateTime.Now, msg);
            writer.Write(fullMsg);
            writer.Close();
        }

        static string Create2Digits(int n)
        {
            return (n < 10 ? "0" : "") + n.ToString();
        }
    }
}
