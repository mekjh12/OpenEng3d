using System.IO;

namespace ZetaExt
{
    public static class LogProfile
    {
        static string _fileName = "";
        static StreamWriter sw = null;

        public static void Create(string filename) 
        { 
            _fileName = filename;
            sw = new StreamWriter(filename);
        }

        public static void Write(string txt)
        {
            sw.Write(txt);
        }

        public static void WriteLine(string txt)
        {
            sw.WriteLine(txt);
        }

        public static void Dispose()
        {
            sw.Close();
            sw.Dispose();
        }
    }
}
