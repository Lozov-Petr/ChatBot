using System;
using System.IO;

namespace ChatBot
{
    public class Logger
    {
        readonly string _path;

        public Logger()
        {
            if (!Directory.Exists("Logs")) Directory.CreateDirectory("Logs");
            var now = DateTime.Now;
            _path = string.Format("Logs/Log {0}-{1}-{2}--{3}-{4}-{5}.log",
                now.Year, Create2Digits(now.Month), Create2Digits(now.Day),
                Create2Digits(now.Hour), Create2Digits(now.Minute), Create2Digits(now.Second));
        }

        public void WriteInLog(string msg)
        {
            var writer  = new StreamWriter(_path, true);
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
