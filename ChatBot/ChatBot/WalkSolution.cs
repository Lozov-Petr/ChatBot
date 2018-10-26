using System;
using System.IO;

namespace ChatBot
{
    public class WalkSolution
    {

        const string _no = "NO";
        const string _yes = "YES";

        readonly string _fileName;

        public WalkSolution(string fileName)
        {
            _fileName = fileName;
        }

        public bool WasWalk
        {
            get
            {
                if (File.Exists(_fileName))
                {
                    var reader = new StreamReader(_fileName);
                    var str = reader.ReadToEnd();
                    reader.Close();
                    return str != _no;
                }
                else
                {
                    var writer = new StreamWriter(_fileName);
                    writer.Write(_no);
                    writer.Close();
                    return false;
                }
            }

            set
            {
                StreamWriter writer = null;
                writer = new StreamWriter(_fileName, false);
                writer.Write(value ? _yes : _no);
                writer.Close();
            }
        }

        public void ReCreate()
        {
            WasWalk = false;
        }
    }
}
