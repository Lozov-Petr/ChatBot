using System;
using System.IO;

namespace ChatBot
{
    public static class Reader
    {

        const string _no = "NO";

        public static bool ReadFrom(string fileName)
        {
            if (File.Exists(fileName))
            {
                var reader = new StreamReader(fileName);
                var str = reader.ReadToEnd();
                reader.Close();
                return str != _no;
            }
            else
            {
                var stream = File.Create(fileName);
                var writer = new StreamWriter(stream);
                writer.Write(_no);
                writer.Close();
                return false;
            }
        }

        public static void ReCreate(string fileName)
        {
            if (File.Exists(fileName)) File.Delete(fileName);

            var stream = File.Create(fileName);
            var writer = new StreamWriter(stream);
            writer.Write(_no);
            writer.Close();
        }
    }
}
